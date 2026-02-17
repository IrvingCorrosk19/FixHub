using FixHub.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages;

public class HealthModel(IFixHubApiClient apiClient) : PageModel
{
    public HealthResponse? ApiHealth { get; private set; }

    public async Task OnGetAsync()
    {
        ApiHealth = await apiClient.GetHealthAsync();
    }
}
