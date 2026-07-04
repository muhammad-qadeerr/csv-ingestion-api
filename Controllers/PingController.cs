using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CsvIngestionApi.Controllers;

/// <summary>
/// Unauthenticated health/ping endpoint to quickly verify the service is running
/// (handy right after deploying to App Service, before dealing with tokens).
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/ping")]
public sealed class PingController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public PingController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        service = "CsvIngestionApi",
        environment = _environment.EnvironmentName,
        utcTime = DateTimeOffset.UtcNow
    });
}
