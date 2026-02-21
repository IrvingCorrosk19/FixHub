using FixHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages.Requests;

[Authorize]
public class ConfirmationModel(IFixHubApiClient apiClient) : PageModel
{
    public Guid JobId { get; set; }

    // FASE 14 IDOR FIX: Verificar que el job pertenece al usuario actual.
    // No confiar en el id recibido en querystring sin validar ownership.
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!SessionUser.IsCustomer(User))
        {
            TempData["Error"] = "Solo los clientes pueden ver esta página.";
            return RedirectToPage("/Index");
        }

        var jobResult = await apiClient.GetJobAsync(id);
        if (!jobResult.IsSuccess || jobResult.Value is null)
        {
            TempData["Error"] = "Solicitud no encontrada.";
            return RedirectToPage("/Index");
        }

        var currentUserId = SessionUser.GetUserId(User);
        if (jobResult.Value.CustomerId != currentUserId)
            return Forbid();

        JobId = id;
        return Page();
    }
}
