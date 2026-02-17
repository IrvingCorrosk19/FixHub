using FixHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages.Jobs;

[Authorize]
public class IndexModel(IFixHubApiClient apiClient) : PageModel
{
    public PagedResult<JobDto>? Jobs { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsCustomer => SessionUser.IsCustomer(User);

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int? Status { get; set; }

    public int? StatusFilter => Status;

    public async Task OnGetAsync()
    {
        var result = await apiClient.ListJobsAsync(Page, 12, Status);

        if (result.IsSuccess)
            Jobs = result.Value;
        else
            ErrorMessage = result.ErrorMessage;
    }
}
