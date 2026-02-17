namespace FixHub.API.Middleware;

/// <summary>
/// Añade headers de seguridad HTTP globales (FASE 5.2).
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
            return Task.CompletedTask;
        });

        await next(context);
    }
}
