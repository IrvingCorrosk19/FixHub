using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FixHub.Web.Helpers;
using FixHub.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages.Account;

public class RegisterModel(IFixHubApiClient apiClient) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Correo no válido.")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "Mínimo 8 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [Range(1, 2, ErrorMessage = "Selecciona un tipo de cuenta.")]
        public int Role { get; set; } = 1;
    }

    public void OnGet()
    {
        if (Request.Query["role"].FirstOrDefault() is { } r && int.TryParse(r, out var role) && (role == 1 || role == 2))
            Input.Role = role;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await apiClient.RegisterAsync(new RegisterRequest(
            Input.FullName,
            Input.Email,
            Input.Password,
            Input.Role,
            Input.Phone));

        if (!result.IsSuccess)
        {
            ErrorMessage = ErrorMessageHelper.GetUserFriendlyMessage(result.ErrorMessage, result.StatusCode);
            return Page();
        }

        // Auto-login tras registro exitoso
        var auth = result.Value!;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.UserId.ToString()),
            new(ClaimTypes.Name, auth.FullName),
            new(ClaimTypes.Email, auth.Email),
            new(ClaimTypes.Role, auth.Role),
            new("jwt_token", auth.Token)
        };

        var identity = new ClaimsIdentity(claims, "CookieAuth");
        await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(identity), new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });

        return RedirectToPage("/Jobs/Index");
    }
}
