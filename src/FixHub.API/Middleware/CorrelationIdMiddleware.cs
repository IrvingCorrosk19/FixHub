namespace FixHub.API.Middleware;

/// <summary>
/// FASE 5.3: Lee X-Correlation-Id del request o genera uno; lo propaga en respuesta y en logging scope.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string CorrelationIdItemKey = "CorrelationId";
    public const string CorrelationIdHeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Items[CorrelationIdItemKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
                context.Response.Headers.Append(CorrelationIdHeaderName, correlationId);
            return Task.CompletedTask;
        });

        var logger = context.RequestServices.GetService<ILogger<CorrelationIdMiddleware>>();
        using (logger?.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
