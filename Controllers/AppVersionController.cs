using Microsoft.AspNetCore.Mvc;

namespace BinayatiBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppVersionController : ControllerBase
{
    [HttpGet]
    public IActionResult GetVersion()
    {
        return Ok(new
        {
            latestVersion = "1.1.0",
            downloadUrl = "https://github.com/MohamedAlksas/binayati-app/releases/download/v1.1.0/app-release.apk",
            forceUpdate = false,
        });
    }
}
