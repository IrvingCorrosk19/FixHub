using FixHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages.Technicians;

[Authorize]
public class ProfileModel(IFixHubApiClient apiClient) : PageModel
{
    public TechnicianProfileDto? Profile { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var result = await apiClient.GetTechnicianProfileAsync(id);

        if (result.IsSuccess)
            Profile = result.Value;

        return Page();
    }
}
