using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FixHub.Web.Helpers;
using FixHub.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages.Account;

public class LoginModel(IFixHubApiClient apiClient) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Correo no válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Password { get; set; } = string.Empty;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await apiClient.LoginAsync(new LoginRequest(Input.Email, Input.Password));

        if (!result.IsSuccess)
        {
            ErrorMessage = ErrorMessageHelper.GetUserFriendlyMessage(result.ErrorMessage, result.StatusCode);
            return Page();
        }

        var auth = result.Value!;
        await SignInAsync(auth);
        return RedirectToPage("/Jobs/Index");
    }

    private async Task SignInAsync(AuthResponse auth)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.UserId.ToString()),
            new(ClaimTypes.Name, auth.FullName),
            new(ClaimTypes.Email, auth.Email),
            new(ClaimTypes.Role, auth.Role),
            new("jwt_token", auth.Token)          // JWT guardado como claim en la cookie
        };

        var identity = new ClaimsIdentity(claims, "CookieAuth");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("CookieAuth", principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });
    }
}
