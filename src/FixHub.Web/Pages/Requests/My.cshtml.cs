using FixHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages.Requests;

[Authorize]
public class MyModel(IFixHubApiClient apiClient) : PageModel
{
    public PagedResult<JobDto>? Jobs { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!SessionUser.IsCustomer(User))
        {
            TempData["Error"] = "Solo los clientes pueden ver sus solicitudes.";
            return RedirectToPage("/Index");
        }

        var result = await apiClient.ListMyJobsAsync(Page, 15);
        if (result.IsSuccess)
            Jobs = result.Value;
        else
            ErrorMessage = result.ErrorMessage;

        return Page();
    }

    /// <summary>Estado en lenguaje cliente (solo mis solicitudes, sin "Por: otro cliente").</summary>
    public static string StatusLabel(string status) => status switch
    {
        "Open" => "Recibida",
        "Assigned" => "Técnico asignado / En progreso",
        "InProgress" => "Técnico asignado / En progreso",
        "Completed" => "Finalizada",
        "Cancelled" => "Cancelada",
        _ => status
    };

    public static string StatusBadge(string status) => status switch
    {
        "Open" => "bg-success",
        "Assigned" => "bg-primary",
        "InProgress" => "bg-warning text-dark",
        "Completed" => "bg-secondary",
        "Cancelled" => "bg-danger",
        _ => "bg-light text-dark"
    };
}
