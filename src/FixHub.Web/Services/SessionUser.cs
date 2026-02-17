using System.Security.Claims;

namespace FixHub.Web.Services;

/// <summary>
/// Helper que lee los claims del usuario autenticado via cookie.
/// Los claims son almacenados al hacer login (ver Account/Login.cshtml.cs).
/// </summary>
public static class SessionUser
{
    public static Guid GetUserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    public static string GetToken(ClaimsPrincipal user) =>
        user.FindFirstValue("jwt_token") ?? string.Empty;

    public static string GetRole(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public static string GetFullName(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public static bool IsCustomer(ClaimsPrincipal user) => GetRole(user) == "Customer";
    public static bool IsTechnician(ClaimsPrincipal user) => GetRole(user) == "Technician";
}
