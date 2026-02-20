using FixHub.Web.Helpers;
using FixHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class IssuesModel(IFixHubApiClient apiClient) : PageModel
{
    public PagedResult<IssueDto>? Issues { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync()
    {
        var result = await apiClient.ListJobIssuesAsync(PageNumber, 20);
        if (result.IsSuccess)
            Issues = result.Value;
        else
            ErrorMessage = result.IsSuccess ? null : ErrorMessageHelper.GetUserFriendlyMessage(result.ErrorMessage, result.StatusCode);
    }

    /// <summary>Etiqueta legible para la razón del reporte.</summary>
    public static string ReasonLabel(string reason) => reason switch
    {
        "no_contact"  => "Sin contacto del técnico",
        "late"        => "Técnico con retraso",
        "bad_service" => "Servicio deficiente",
        "other"       => "Otro",
        _             => reason
    };

    public static string ReasonBadgeClass(string reason) => reason switch
    {
        "no_contact"  => "bg-warning text-dark",
        "late"        => "bg-warning text-dark",
        "bad_service" => "bg-danger",
        "other"       => "bg-secondary",
        _             => "bg-light text-dark"
    };
}
