using Microsoft.AspNetCore.Mvc;

namespace FixHub.API.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>Health check endpoint — no requiere autenticación.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new HealthResponse(
        Status: "healthy",
        Version: "1.0.0",
        Timestamp: DateTime.UtcNow
    ));

    public record HealthResponse(string Status, string Version, DateTime Timestamp);
}
