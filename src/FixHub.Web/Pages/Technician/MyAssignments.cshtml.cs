using FixHub.Web.Helpers;
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
            ErrorMessage = result.IsSuccess ? null : ErrorMessageHelper.GetUserFriendlyMessage(result.ErrorMessage, result.StatusCode);

        return Page();
    }

    public static string StatusBadge(string status) => StatusHelper.Badge(status);
    public static string StatusLabel(string status) => StatusHelper.Label(status);
}
