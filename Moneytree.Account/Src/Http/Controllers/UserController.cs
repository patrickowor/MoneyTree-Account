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
using Moneytree.Account.Src.Internal.Schemas;
using Moneytree.Account.Src.Modules;
using Moneytree.AccountContracts.User;

[ApiController]
[Route("user")]
public class UserController(Db dbContext, AppStore store, Helpers utils, EnvSchema env) : ControllerBase
{

    private readonly Db _DbContext = dbContext;
    private readonly IDatabase _AppStore = store.GetDatabase();
    private readonly EnvSchema _env = env;
    private readonly Helpers _Utils = utils;

    [HttpGet()]
    [Authorize]
    async public Task<IActionResult> GetUserInfo()
    {
        TokenInfo? claim = (TokenInfo?)HttpContext.Items["user"];

        if (claim == null || claim.UserId == null || claim?.TokenType == JwtTokenEnum.LoginToken.ToString())
        {
            return Unauthorized();
        }
        Guid userId = Guid.Parse(claim?.UserId ?? "");

        UserModel user = await _DbContext.Users.FirstAsync(u => u.Id == userId);

        var userData = _Utils.OmitFields(user, ["LastLogInAttempt", "FailedLoginAttempt", "Passcode"]);

        return Ok(new ServerResponse { Data = userData });
    }


    [HttpGet("refferal")]
    [Authorize]
    async public Task<IActionResult> GetRefferals()
    {
        TokenInfo? claim = (TokenInfo?)HttpContext.Items["user"];

        if (claim == null || claim.UserId == null || claim?.TokenType == JwtTokenEnum.LoginToken.ToString())
        {
            return Unauthorized();
        }
        Guid userId = Guid.Parse(claim?.UserId ?? "");

        UserModel user = await _DbContext.Users.FirstAsync(u => u.Id == userId);

        if (user.RefferalCode == null)
        {
            return BadRequest(new ServerResponse { Message = "refferal code not found" });
        }

        var reffered = await _DbContext.Users.Where(u => u.Refferer == user.RefferalCode).Select(u => new
        {
            u.Id,
            u.FirstName,
            u.LastName
        }).ToListAsync();



        return Ok(new ServerResponse { Data = reffered });
    }

    [HttpPost("profile-image")]
    [Authorize]
    public IActionResult UpdateProfileImage(UpdateProfileImage payload)
    {
        TokenInfo? claim = (TokenInfo?)HttpContext.Items["user"];

        if (claim == null || claim.UserId == null || claim?.TokenType == JwtTokenEnum.LoginToken.ToString())
        {
            return Unauthorized();
        }
        Guid userId = Guid.Parse(claim?.UserId ?? "");

        string mime_type;
        string image_body;
        String[] img_split = payload.Base64Image.Split(",");
        if (img_split.Length <= 1 && payload.MimeType == null)
        {
            return BadRequest(new ServerResponse { Message = "mime_type not provided" });
        }
        else if (payload.MimeType != null)
        {
            mime_type = payload.MimeType;
            image_body = img_split.Last();
        }
        else
        {
            mime_type = img_split[0].Split(";").First().Split(":")[1];
            image_body = img_split[1];
        }

        // TODO : send image to upload queue
        Console.WriteLine($"{mime_type} {image_body}");
        return Accepted(new ServerResponse { });
    }
    

    [HttpPost("update-email")]
    [Authorize]
    public IActionResult StartEmailUpdate(EmailRequest payload)
    {
        return Ok(new ServerResponse { });
    }

    [HttpPatch("update-email")]
    [Authorize]
    public IActionResult CompleteEmailUpdate(VerifyOtpRequest payload)
    {
        return Ok(new ServerResponse { });
    }
    

    [HttpPost("update-phonenumber")]
    [Authorize]
    public IActionResult StartPhoneUpdate(PhoneNumberRequest payload)
    {
        return Ok(new ServerResponse { });
    }
    
    [HttpPatch("update-phonenumber")]
    [Authorize]
    public IActionResult CompletePhoneUpdate(VerifyOtpRequest payload)
    { 
         return Ok(new ServerResponse { });
    }
}