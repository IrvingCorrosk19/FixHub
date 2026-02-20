using FixHub.Web.Helpers;
using FixHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages.Jobs;

[Authorize]
public class IndexModel(IFixHubApiClient apiClient, ILogger<IndexModel> logger) : PageModel
{
    public PagedResult<JobDto>? Jobs { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsCustomer => SessionUser.IsCustomer(User);
    public bool IsTechnician => SessionUser.IsTechnician(User);

    [BindProperty(SupportsGet = true, Name = "page")]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int? Status { get; set; }

    public int? StatusFilter => Status;

    // Technician dashboard
    public int KpiOpen { get; set; }
    public int KpiAssigned { get; set; }
    public int KpiCompleted { get; set; }
    public decimal KpiEarnings { get; set; }
    public PagedResult<JobDto>? OpenJobs { get; set; }
    public List<AssignmentDto> AssignedList { get; private set; } = new();
    public List<AssignmentDto> CompletedList { get; private set; } = new();
    public TechnicianProfileDto? MyProfile { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Customer: FixHub es empresa, no marketplace; no ver feed de trabajos de otros.
        if (IsCustomer)
            return RedirectToPage("/Requests/My");

        if (IsTechnician)
        {
            await LoadTechnicianDashboardAsync();
            return Page();
        }

        var result = await apiClient.ListJobsAsync(PageNumber, 12, Status);
        if (result.IsSuccess)
            Jobs = result.Value;
        else
            ErrorMessage = ErrorMessageHelper.GetUserFriendlyMessage(result.ErrorMessage, result.StatusCode);
        return Page();
    }

    private async Task LoadTechnicianDashboardAsync()
    {
        // status 1 = Open (trabajos disponibles para enviar propuesta)
        var openResult = await apiClient.ListJobsAsync(1, 50, 1);
        var assignResult = await apiClient.GetMyAssignmentsAsync(1, 100);

        if (openResult.IsSuccess && openResult.Value != null)
        {
            OpenJobs = openResult.Value;
            KpiOpen = openResult.Value.TotalCount;
            logger.LogInformation("Jobs/Index (Technician): API devolvió {Count} oportunidades abiertas (TotalCount={Total})",
                openResult.Value.Items?.Count ?? 0, openResult.Value.TotalCount);
        }
        else if (!openResult.IsSuccess)
        {
            logger.LogWarning("Jobs/Index (Technician): fallo al cargar oportunidades. Mensaje: {Message}", openResult.ErrorMessage);
            ErrorMessage = ErrorMessageHelper.GetUserFriendlyMessage(openResult.ErrorMessage, openResult.StatusCode);
        }

        if (assignResult.IsSuccess && assignResult.Value != null)
        {
            var items = assignResult.Value.Items;
            AssignedList = items.Where(a => a.JobStatus == "Assigned" || a.JobStatus == "InProgress").ToList();
            CompletedList = items.Where(a => a.JobStatus == "Completed").ToList();
            KpiAssigned = AssignedList.Count;
            KpiCompleted = CompletedList.Count;
            KpiEarnings = CompletedList.Sum(a => a.AcceptedPrice);
        }
        else if (!assignResult.IsSuccess)
            ErrorMessage = ErrorMessageHelper.GetUserFriendlyMessage(assignResult.ErrorMessage, assignResult.StatusCode);

        var profileResult = await apiClient.GetTechnicianProfileAsync(SessionUser.GetUserId(User));
        if (profileResult.IsSuccess)
            MyProfile = profileResult.Value;
    }

    public static string StatusBadge(string status) => StatusHelper.Badge(status);
    public static string StatusLabel(string status) => StatusHelper.Label(status);
}
