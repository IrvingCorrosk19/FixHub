using FixHub.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace FixHub.API.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController(IDatabaseHealthChecker dbHealth) : ControllerBase
{
    /// <summary>Health check endpoint — no requiere autenticación. FASE 9: verifica conexión a DB.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var dbOk = await dbHealth.CanConnectAsync(ct);
        var response = new HealthResponse(
            Status: dbOk ? "healthy" : "unhealthy",
            Version: "1.0.0",
            Timestamp: DateTime.UtcNow,
            Database: dbOk ? "connected" : "disconnected"
        );
        return dbOk ? Ok(response) : StatusCode(503, response);
    }

    public record HealthResponse(string Status, string Version, DateTime Timestamp, string? Database = null);
}
