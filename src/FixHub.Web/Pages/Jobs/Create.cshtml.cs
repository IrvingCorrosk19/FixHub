using System.ComponentModel.DataAnnotations;
using FixHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages.Jobs;

[Authorize]
public class CreateModel(IFixHubApiClient apiClient) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Selecciona una categoría.")]
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(200, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(2000, MinimumLength = 20)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        [StringLength(500)]
        public string AddressText { get; set; } = string.Empty;

        [Range(0, 999999)]
        public decimal? BudgetMin { get; set; }

        [Range(0, 999999)]
        public decimal? BudgetMax { get; set; }
    }

    public IActionResult OnGet()
    {
        if (!SessionUser.IsCustomer(User))
        {
            TempData["Error"] = "Solo los clientes pueden publicar trabajos. Inicia sesión con una cuenta de tipo Cliente o regístrate como Cliente.";
            return RedirectToPage("/Jobs/Index");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!SessionUser.IsCustomer(User))
        {
            TempData["Error"] = "Solo los clientes pueden publicar trabajos.";
            return RedirectToPage("/Jobs/Index");
        }
        if (!ModelState.IsValid) return Page();

        var result = await apiClient.CreateJobAsync(new CreateJobRequest(
            Input.CategoryId,
            Input.Title,
            Input.Description,
            Input.AddressText,
            null, null,
            Input.BudgetMin,
            Input.BudgetMax));

        if (!result.IsSuccess)
        {
            ErrorMessage = result.StatusCode == 403
                ? "Solo los clientes pueden publicar trabajos. Tu cuenta es de técnico."
                : (result.ErrorMessage ?? "Error al publicar el trabajo.");
            return Page();
        }

        TempData["Success"] = "Trabajo publicado correctamente.";
        return RedirectToPage("/Jobs/Detail", new { id = result.Value!.Id });
    }
}
