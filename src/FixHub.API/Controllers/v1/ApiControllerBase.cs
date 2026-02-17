using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace FixHub.API.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Lee el UserId del claim "sub" del JWT.
    /// JwtBearer (MapInboundClaims=true, default) mapea sub → NameIdentifier.
    /// Fallback a "sub" literal por compatibilidad con distintas librerías.
    /// </summary>
    protected Guid CurrentUserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(raw, out var id))
                throw new InvalidOperationException("User ID claim missing or invalid.");
            return id;
        }
    }

    protected string CurrentUserRole =>
        User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    protected bool IsAdmin => CurrentUserRole == "Admin";
}
