using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FixHub.API.Middleware;

/// <summary>
/// FASE 14: Añade UserId y JobId al scope de logging cuando están disponibles.
/// Debe ejecutarse después de UseAuthentication.
/// </summary>
public class RequestContextLoggingMiddleware(RequestDelegate next)
{
    public const string JobIdItemKey = "RequestJobId";

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.User?.FindFirst("sub")?.Value;

        var jobId = context.Request.RouteValues["id"]?.ToString();
        if (!string.IsNullOrEmpty(jobId) && Guid.TryParse(jobId, out _))
            context.Items[JobIdItemKey] = jobId;

        var logger = context.RequestServices.GetService<ILogger<RequestContextLoggingMiddleware>>();
        var dict = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(userId)) dict["UserId"] = userId;
        if (!string.IsNullOrEmpty(jobId)) dict["JobId"] = jobId;

        if (dict.Count > 0 && logger != null)
        {
            using (logger.BeginScope(dict))
            {
                await next(context);
            }
        }
        else
        {
            await next(context);
        }
    }
}
