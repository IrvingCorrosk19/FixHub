using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace FixHub.Web.Services;

/// <summary>
/// DelegatingHandler que lee el JWT de la cookie de sesión y lo agrega
/// automáticamente como "Authorization: Bearer {token}" en cada request
/// al API. Así las PageModels no necesitan pasar el token manualmente.
/// </summary>
public class BearerTokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx?.User?.Identity?.IsAuthenticated == true)
        {
            var token = ctx.User.FindFirstValue("jwt_token");
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
