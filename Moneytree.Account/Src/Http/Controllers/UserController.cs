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

[ApiController]
[Route("user")]
public class UserController(Db dbContext, AppStore store, Helpers utils, EnvSchema env) : ControllerBase
{

    private readonly Db _DbContext = dbContext;
    private readonly IDatabase _AppStore = store.GetDatabase();
    private readonly EnvSchema _env = env;
    private readonly Helpers _Utils = utils;

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
            return BadRequest(new ServerResponse{ Message = "refferal code not found" });
        }

        var reffered = await _DbContext.Users.Where(u => u.Refferer == user.RefferalCode).Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName
            }).ToListAsync();

        

        return Ok(new ServerResponse { Data = reffered } );
    }
}