namespace Moneytree.Src.Http.Controllers;

using Moneytree.AccountContracts.Auth;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    [HttpPost("signup")]
    public IActionResult InitiateSignup(EmailRequest payload){

        return Ok(payload);
    }
}