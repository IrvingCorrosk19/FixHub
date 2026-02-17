using FixHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages.Technician;

[Authorize]
public class MyAssignmentsModel(IFixHubApiClient apiClient) : PageModel
{
    public PagedResult<AssignmentDto>? Assignments { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!SessionUser.IsTechnician(User))
            return RedirectToPage("/Jobs/My");

        var result = await apiClient.GetMyAssignmentsAsync(Page, 15);

        if (result.IsSuccess)
            Assignments = result.Value;
        else
            ErrorMessage = result.ErrorMessage;

        return Page();
    }

    public static string StatusBadge(string status) => status switch
    {
        "Open"       => "bg-success",
        "Assigned"   => "bg-primary",
        "InProgress" => "bg-warning text-dark",
        "Completed"  => "bg-secondary",
        "Cancelled"  => "bg-danger",
        _            => "bg-light text-dark"
    };

    public static string StatusLabel(string status) => status switch
    {
        "Open"       => "Abierto",
        "Assigned"   => "Asignado",
        "InProgress" => "En progreso",
        "Completed"  => "Completado",
        "Cancelled"  => "Cancelado",
        _            => status
    };
}
