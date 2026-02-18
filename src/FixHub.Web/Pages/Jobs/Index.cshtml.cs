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
    public bool IsTechnician => SessionUser.IsTechnician(User);

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

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

    public async Task OnGetAsync()
    {
        if (IsTechnician)
        {
            await LoadTechnicianDashboardAsync();
            return;
        }

        var result = await apiClient.ListJobsAsync(Page, 12, Status);
        if (result.IsSuccess)
            Jobs = result.Value;
        else
            ErrorMessage = result.ErrorMessage;
    }

    private async Task LoadTechnicianDashboardAsync()
    {
        var openResult = await apiClient.ListJobsAsync(1, 50, 1);
        var assignResult = await apiClient.GetMyAssignmentsAsync(1, 100);

        if (openResult.IsSuccess && openResult.Value != null)
        {
            OpenJobs = openResult.Value;
            KpiOpen = openResult.Value.TotalCount;
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
            ErrorMessage = assignResult.ErrorMessage;

        var profileResult = await apiClient.GetTechnicianProfileAsync(SessionUser.GetUserId(User));
        if (profileResult.IsSuccess)
            MyProfile = profileResult.Value;
    }

    public static string StatusBadge(string status) => status switch
    {
        "Open" => "bg-success",
        "Assigned" => "bg-primary",
        "InProgress" => "bg-warning text-dark",
        "Completed" => "bg-secondary",
        "Cancelled" => "bg-danger",
        _ => "bg-light text-dark"
    };

    public static string StatusLabel(string status) => status switch
    {
        "Open" => "Abierto",
        "Assigned" => "Asignado",
        "InProgress" => "En progreso",
        "Completed" => "Completado",
        "Cancelled" => "Cancelado",
        _ => status
    };
}
