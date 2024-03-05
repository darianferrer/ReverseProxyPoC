using Microsoft.AspNetCore.Mvc;

namespace ReverseProxyPoC.Controllers;

[ApiController]
[Route("version")]
public class VersionController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("1.0.0");
    }
}
