using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tracker.Shared.Configuration;

namespace Tracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppSettingsController : ControllerBase
{
    private readonly AppSettings _appSettings;

    public AppSettingsController(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_appSettings);
    }
}
