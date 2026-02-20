using FixHub.Web.Helpers;
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
            ErrorMessage = result.IsSuccess ? null : ErrorMessageHelper.GetUserFriendlyMessage(result.ErrorMessage, result.StatusCode);

        return Page();
    }

    public static string StatusLabel(string status) => StatusHelper.Label(status);
    public static string StatusBadge(string status) => StatusHelper.Badge(status);
}
