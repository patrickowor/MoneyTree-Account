namespace Moneytree.Account.Src.Http.Controllers;

using Moneytree.AccountContracts.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moneytree.Account.Src.Entities;
using Moneytree.Account.Src.Config;
using StackExchange.Redis;
using System.Text.Json;
using Moneytree.Account.Src.Utils;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Moneytree.Account.Src.Internal.Schemas;
using Microsoft.OpenApi.Extensions;
using Moneytree.Account.Src.Modules;
using Microsoft.AspNetCore.Authentication.JwtBearer;

[ApiController]
[Route("auth")]
public class AuthController(Db dbContext, AppStore store, Helpers utils, EnvSchema env) : ControllerBase
{
    private readonly Db _DbContext = dbContext;
    private readonly IDatabase _AppStore = store.GetDatabase();
    private readonly EnvSchema _env = env;
    private readonly Helpers _Utils = utils;

    [HttpPost("signup")]
    async public Task<IActionResult> InitiateSignup(EmailRequest payload)
    {
        UserModel? user = await _DbContext.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

        if (user != null) {
            return BadRequest(new {message = "user already exist"});
        }

        var users_key = _Utils.GenerateKey("user", payload.Email);
        String? signup_key = null; 

        String? otp = null;
        bool has_otp = false;

        String? store_response = await _AppStore.StringGetAsync(users_key);
        if (store_response != null)
        {
            signup_key = store_response;
            String? user_string = await _AppStore.StringGetAsync(signup_key);

            if (user_string != null)
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(user_string);
                has_otp = data != null && data["otp"] != null;
                otp = data?["otp"] ?? _Utils.GenerateOtp();
            }
        }

        if (!has_otp || signup_key == null || otp == null)
        {
            signup_key = _Utils.GenerateKey("create_user");

            otp = _Utils.GenerateOtp();

            var userInfo = new Dictionary<string, string>
            {
                ["otp"] = otp,
                ["email"] = payload.Email,
            };


            await _AppStore.StringSetAsync(
                signup_key,
                JsonSerializer.Serialize(userInfo),
                TimeSpan.FromMinutes(10)
            );

            await _AppStore.StringSetAsync(
                users_key,
                signup_key,
                TimeSpan.FromMinutes(10)
            );

        }
        if (_env.SERVICE_ENV != "production") Response.Headers.Append("get-otp", otp );
        // TODO: send otp email via queue

        // splitting the signup key to get the reference token/ flow key
        string flow_key = signup_key.Split(":")[1];
        return Ok(new { message = "success", data = new { flow_key } });
    }

    [HttpPost("otp")]
    async public Task<IActionResult> VerifyOtp(VerifyOtpRequest payload)
    {
        // this is the flowkey provided
        var signup_key = _Utils.GenerateKey("create_user", payload.ReferenceToken);

        if (signup_key == null)
        {
            return BadRequest(new { message = "invalid otp" });
        }

        String? user_string = await _AppStore.StringGetAsync(signup_key);

        if (user_string == null)
        {
            return BadRequest(new { message = "invalid otp" });
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(user_string);

        var isInvalid = data == null || data["otp"] != payload.Otp;

        if (isInvalid)
        {
            return BadRequest(new { message = "invalid otp" });
        }

        var user_id = data?["email"] ?? "";

        var token = _Utils.GenerateJwtToken(user_id, Internal.Schemas.JwtTokenEnum.SignupToken);

        return Ok(new { message = "success", data = new { token } });
    }

    [HttpPatch("signup")]
    [Authorize]
    async public Task<IActionResult> CompleteProfile(SignupCompleteProfileRequest payload)
    {
        TokenInfo? claim = (TokenInfo?)HttpContext.Items["user"];
        if (claim?.TokenType != JwtTokenEnum.SignupToken.ToString() || claim?.UserId == null)
        {
            return StatusCode(403, new { message = "Invalid token type" });
        }

        var user = new UserModel
        {
            Id = Guid.NewGuid(),
            FirstName = payload.FirstName,
            LastName = payload.LastName,
            MiddleName = payload.MiddleName,
            PhoneNumber = payload.PhoneNumber,
            Email = claim?.UserId,
            Refferer = payload.Refferer,
            RefferalCode = _Utils.GenerateNTokens(7)
        };

        await _DbContext.Users.AddAsync(user);
        await _DbContext.SaveChangesAsync();

        return Ok(new { message = "success" });
    }

    [HttpPost("login")]
    async public Task<IActionResult> Login(LoginRequest payload)
    {
        UserModel? user = await _DbContext.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

        if (user == null) {
            return BadRequest(new {message = "invalid email address/passcode"});
        }

        if (user.Passcode == null)
        {
            return BadRequest(new {message = "registration incomplete"});
        }

        string login_key = _Utils.GenerateKey("login", user.Id.ToString());

        if (payload.Passcode != null)
        {
            if (!PasswordHasher.VerifyPassword(payload.Passcode, user.Passcode))
            {
                user.LastLogInAttempt = DateTime.Now;
                user.FailedLoginAttempt += 1;
                await _DbContext.SaveChangesAsync();
                return BadRequest(new { message = "invalid email address/passcode" });
            }

            if (user.HasMFA)
            {
                // return data for mfa verification 
                string otp = _Utils.GenerateOtp(6);

                await _AppStore.StringSetAsync(
                    login_key,
                    otp,
                    TimeSpan.FromMinutes(5)
                );

                if (_env.SERVICE_ENV != "production") Response.Headers.Append("get-otp", otp );

                // TODO: send otp email via queue

                return Ok(new { message = "success", data = new { login_type = "2FA" } });

            }

            user.LastLogInAttempt = DateTime.Now;
            user.FailedLoginAttempt = 0;
            await _DbContext.SaveChangesAsync();
        }
        else if (payload.MfaToken != null && user.HasMFA)
        {
            String? otp_string = await _AppStore.StringGetAsync(login_key);
            // if mfa failed return error
            if (otp_string == null || payload.MfaToken != otp_string)
            {
                user.LastLogInAttempt = DateTime.Now;
                user.FailedLoginAttempt += 1;
                await _DbContext.SaveChangesAsync();
                return BadRequest(new { message = "invalid mfa_token" });
            } 
            
            user.LastLogInAttempt = DateTime.Now;
            user.FailedLoginAttempt = 0;
            await _DbContext.SaveChangesAsync();            
        }
        else
        {
            user.LastLogInAttempt = DateTime.Now;
            user.FailedLoginAttempt += 1;
            await _DbContext.SaveChangesAsync();
            return BadRequest(new { message = "passcode/mfa_token not provided" });
        }

        var user_id = user.Id.ToString();

        var token = _Utils.GenerateJwtToken(user_id, Internal.Schemas.JwtTokenEnum.LoginToken);

        return Ok(new { message = "success", data = new { token } });
    }

    [HttpPost("forgot")]
    async public Task<IActionResult> ForgotPassword(EmailRequest payload)
    {
        UserModel? user = await _DbContext.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

        if (user == null) {
            return BadRequest(new {message = "invalid email address"});
        }

        string forgot_passcode_key = _Utils.GenerateKey("forgot-passcode");
        var otp = _Utils.GenerateOtp();
        if (_env.SERVICE_ENV != "production") Response.Headers.Append("get-otp", otp);

        var userInfo = new Dictionary<string, string>
        {
            ["otp"] = otp,
            ["email"] = payload.Email,
        };

        await _AppStore.StringSetAsync(
            forgot_passcode_key,
            JsonSerializer.Serialize(userInfo),
            TimeSpan.FromMinutes(10)
        );

        // TODO: send otp email via queue

        return Ok(new {  message = "success" });
    }

    [HttpPatch("forgot")]
    async public Task<IActionResult> ForgotPasswordVerify(VerifyOtpRequest payload)
    {
        string forgot_passcode_key = _Utils.GenerateKey("forgot-passcode", payload.ReferenceToken);

        if (forgot_passcode_key == null)
        {
            return BadRequest(new { message = "invalid otp" });
        }

        String? cached_string = await _AppStore.StringGetAsync(forgot_passcode_key);

        if (cached_string == null)
        {
            return BadRequest(new { message = "invalid otp" });
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(cached_string);

        var isInvalid = data == null || data["otp"] != payload.Otp;

        if (isInvalid)
        {
            return BadRequest(new { message = "invalid otp" });
        }

        var user_id = data?["email"] ?? "";

        var token = _Utils.GenerateJwtToken(user_id, Internal.Schemas.JwtTokenEnum.ForgotToken);

        return Ok(new { message = "success", data = new { token } });
    }

    [HttpPost("change-password")]
    [Authorize]
    async public Task<IActionResult> ChangePassword(ChangePasswordRequest payload)
    {
        TokenInfo? claim = (TokenInfo?)HttpContext.Items["user"];
        if (claim?.UserId == null || claim?.TokenType != JwtTokenEnum.LoginToken.ToString())
        {
            return StatusCode(403, new { message = "Invalid token type" });
        }

        UserModel user = await _DbContext.Users.FirstAsync(u => u.Id == Guid.Parse(claim.UserId));

        // check failed login attempt is less than 3 in the last 30 minuites 
        if (user.FailedLoginAttempt >= 3 && (DateTime.Now - user.LastLogInAttempt).TotalMinutes < 30)
        {
            return StatusCode(403, new { message = "Too many failed login attempts. Please wait 30 minutes before trying again." });
        }

        if (!PasswordHasher.VerifyPassword(payload.CurrentPasscode, user.Passcode ?? ""))
        {
            user.LastLogInAttempt = DateTime.Now;
            user.FailedLoginAttempt += 1;
            await _DbContext.SaveChangesAsync();
            return BadRequest(new { message = "Invalid token type" });
        }


        user.LastLogInAttempt = DateTime.Now;
        user.FailedLoginAttempt = 0;
        await _DbContext.SaveChangesAsync();

        String change_password_key = _Utils.GenerateKey("create_user", claim.UserId);

        await _AppStore.StringSetAsync(
            change_password_key,
            change_password_key,
            TimeSpan.FromMinutes(5)
        );

        return Ok(payload);
    }

    [HttpPatch("set-password")]
    [Authorize]
    async public Task<IActionResult> SetPassword(SetPasswordRequest payload)
    {
        // this uses a special auth token for setting password so that users have to go through legitimate means to set thier password which include
        // create a profile then set password
        // forget password, verify otp then set password
        // Init password change then set password

        if (payload.NewPasscode != payload.ConfirmPasscode)
        {
            return BadRequest(new { message = "new passcode doesnt match" });
        }

        TokenInfo? claim = (TokenInfo?)HttpContext.Items["user"];

        if (
                claim?.TokenType == JwtTokenEnum.SignupToken.ToString() ||
                claim?.TokenType == JwtTokenEnum.ForgotToken.ToString()
            )
        {
            UserModel? user = await _DbContext.Users.FirstOrDefaultAsync(u => u.Email == claim.UserId);

            if (user == null)
            {
                return BadRequest(new { message = "complete user signup proccess before including password" });
            }

            if (user.Passcode != null && claim?.TokenType == JwtTokenEnum.SignupToken.ToString())
            {
                return BadRequest(new { message = "user signup completed!, please login to change password" });
            }
            else if (claim?.TokenType == JwtTokenEnum.SignupToken.ToString())
            {
                user.Status = UserStatus.Active;
            }

            user.Passcode = PasswordHasher.HashPassword(payload.NewPasscode);
            await _DbContext.SaveChangesAsync();
        }
        else if (claim?.TokenType == JwtTokenEnum.LoginToken.ToString())
        {
            UserModel? user = await _DbContext.Users.FirstOrDefaultAsync(u => u.Id == Guid.Parse(claim.UserId));

            if (user == null)
            {
                return BadRequest(new { message = "invalid user" });
            }
            String change_password_key = _Utils.GenerateKey("create_user", claim.UserId);
            String? cached_data = await _AppStore.StringGetAsync(change_password_key);

            if (cached_data == null)
            {
                return BadRequest(new { message = "Password change request required." }); 
            }
            user.Passcode = PasswordHasher.HashPassword(payload.NewPasscode);
            await _DbContext.SaveChangesAsync();
        }
        else
        {
            return StatusCode(403, new { message = "Invalid token type" });
        }
        return Ok(new { message = "success", data = "successful, please log in to continue." });
    }

}