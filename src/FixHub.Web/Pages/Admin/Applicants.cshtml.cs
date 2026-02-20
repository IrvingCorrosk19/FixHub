using FixHub.Web.Helpers;
using FixHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ApplicantsModel(IFixHubApiClient apiClient) : PageModel
{
    public PagedResult<ApplicantDto>? Applicants { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public string? StatusFilter => Status;

    public async Task OnGetAsync()
    {
        var result = await apiClient.ListApplicantsAsync(PageNumber, 20, Status);
        if (result.IsSuccess)
            Applicants = result.Value;
        else
            ErrorMessage = result.IsSuccess ? null : ErrorMessageHelper.GetUserFriendlyMessage(result.ErrorMessage, result.StatusCode);
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid id)
    {
        var result = await apiClient.UpdateTechnicianStatusAsync(id, 2); // Approved = 2
        if (result.IsSuccess)
            TempData["Success"] = "Técnico aprobado.";
        else
            TempData["Error"] = ErrorMessageHelper.GetUserFriendlyMessage(result.ErrorMessage, result.StatusCode);
        return RedirectToPage(new { page = PageNumber, status = Status });
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid id)
    {
        var result = await apiClient.UpdateTechnicianStatusAsync(id, 3); // Rejected = 3
        if (result.IsSuccess)
            TempData["Success"] = "Técnico rechazado.";
        else
            TempData["Error"] = ErrorMessageHelper.GetUserFriendlyMessage(result.ErrorMessage, result.StatusCode);
        return RedirectToPage(new { page = PageNumber, status = Status });
    }

    public async Task<IActionResult> OnPostInterviewAsync(Guid id)
    {
        var result = await apiClient.UpdateTechnicianStatusAsync(id, 1); // InterviewScheduled = 1
        if (result.IsSuccess)
            TempData["Success"] = "Estado actualizado a entrevista programada.";
        else
            TempData["Error"] = ErrorMessageHelper.GetUserFriendlyMessage(result.ErrorMessage, result.StatusCode);
        return RedirectToPage(new { page = PageNumber, status = Status });
    }

    public static string StatusLabel(string status) => status switch
    {
        "Pending" => "Pendiente",
        "InterviewScheduled" => "Entrevista programada",
        "Approved" => "Aprobado",
        "Rejected" => "Rechazado",
        _ => status
    };

    public static string StatusBadgeClass(string status) => status switch
    {
        "Pending" => "bg-warning text-dark",
        "InterviewScheduled" => "bg-info",
        "Approved" => "bg-success",
        "Rejected" => "bg-secondary",
        _ => "bg-light text-dark"
    };
}
