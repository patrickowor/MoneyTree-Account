using Microsoft.AspNetCore.Mvc;

namespace Moneytree.Account.Src.Http.Controllers;

public class ErrorContoller : ControllerBase
{
    [Route("/error")]
    public IActionResult HandleErrors(){
        return Problem();
    }
}