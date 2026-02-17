using System.Diagnostics;

namespace FixHub.API.Middleware;

/// <summary>
/// FASE 5.3: Logging estructurado por request (Path, StatusCode, elapsedMs). CorrelationId ya en scope.
/// No loguea cuerpo, tokens ni passwords.
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        await next(context);
        sw.Stop();

        logger.LogInformation(
            "Request {Path} completed with {StatusCode} in {ElapsedMs}ms",
            context.Request.Path,
            context.Response.StatusCode,
            sw.ElapsedMilliseconds);
    }
}
