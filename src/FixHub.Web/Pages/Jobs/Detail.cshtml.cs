using FixHub.Web.Helpers;
using FixHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages.Jobs;

[Authorize]
public class DetailModel(IFixHubApiClient apiClient) : PageModel
{
    public JobDto? Job { get; set; }
    public List<ProposalDto>? Proposals { get; set; }
    public Dictionary<Guid, TechnicianProfileDto> TechnicianProfiles { get; set; } = new();
    /// <summary>Técnico asignado al trabajo (para mostrar al cliente cuando ya está asignado).</summary>
    public TechnicianProfileDto? AssignedTechnicianProfile { get; set; }
    public bool HasReview { get; set; }
    public Guid? AssignedTechnicianId { get; set; }

    // Datos del formulario "Reportar problema"
    [BindProperty] public string IssueReason { get; set; } = string.Empty;
    [BindProperty] public string? IssueDetail { get; set; }

    public bool IsCustomer => SessionUser.IsCustomer(User);
    public bool IsTechnician => SessionUser.IsTechnician(User);
    public bool IsAdmin => SessionUser.GetRole(User) == "Admin";
    public bool IsOwner => Job?.CustomerId == SessionUser.GetUserId(User);
    /// <summary>Solo Admin ve la lista de propuestas y puede aceptar (empresa, no marketplace).</summary>
    public bool CanSeeProposals => IsAdmin;

    /// <summary>
    /// Tiempo relativo "hace X minutos" calculado desde CreatedAt del job.
    /// Se usa en la vista para mostrar cuándo fue creada la solicitud.
    /// </summary>
    public string CreatedAgo => RelativeTime(Job?.CreatedAt ?? DateTime.UtcNow);

    /// <summary>ISO 8601 de CreatedAt para el atributo data- del stepper (JS lo usa para animación).</summary>
    public string CreatedAtIso => (Job?.CreatedAt ?? DateTime.UtcNow).ToString("o");

    /// <summary>Tiempo relativo legible.</summary>
    public static string RelativeTime(DateTime utcTime)
    {
        var diff = DateTime.UtcNow - utcTime;
        return diff.TotalSeconds < 60  ? "hace un momento"
             : diff.TotalMinutes < 60  ? $"hace {(int)diff.TotalMinutes} min"
             : diff.TotalHours < 24    ? $"hace {(int)diff.TotalHours} h"
             : diff.TotalDays < 7      ? $"hace {(int)diff.TotalDays} días"
             : utcTime.ToLocalTime().ToString("dd/MM/yyyy");
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        await LoadJobAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid id)
    {
        var result = await apiClient.CompleteJobAsync(id);

        if (!result.IsSuccess)
            TempData["Error"] = ErrorMessageHelper.GetUserFriendlyMessage(result.ErrorMessage, result.StatusCode);
        else
            TempData["Success"] = "✅ Servicio confirmado. ¡Gracias por usar FixHub!";

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id)
    {
        var result = await apiClient.CancelJobAsync(id);

        if (!result.IsSuccess)
            TempData["Error"] = ErrorMessageHelper.GetUserFriendlyMessage(result.ErrorMessage, result.StatusCode);
        else
            TempData["Success"] = "Solicitud cancelada correctamente.";

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReportIssueAsync(Guid id)
    {
        if (string.IsNullOrWhiteSpace(IssueReason))
        {
            TempData["Error"] = "Por favor selecciona el tipo de problema.";
            return RedirectToPage(new { id });
        }

        var result = await apiClient.ReportJobIssueAsync(id, IssueReason, IssueDetail);

        if (!result.IsSuccess)
            TempData["Error"] = ErrorMessageHelper.GetUserFriendlyMessage(result.ErrorMessage, result.StatusCode);
        else
            TempData["Success"] = "✅ Reporte enviado. Nuestro equipo se comunicará contigo a la brevedad.";

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAcceptAsync(Guid id, Guid proposalId)
    {
        var result = await apiClient.AcceptProposalAsync(proposalId);

        if (!result.IsSuccess)
            TempData["Error"] = result.ErrorMessage ?? "Error al aceptar la propuesta.";
        else
            TempData["Success"] = $"✅ Técnico {result.Value?.TechnicianName} asignado correctamente.";

        return RedirectToPage(new { id });
    }

    private async Task LoadJobAsync(Guid id)
    {
        var jobResult = await apiClient.GetJobAsync(id);
        if (!jobResult.IsSuccess || jobResult.Value is null)
        {
            TempData["Error"] = ErrorMessageHelper.GetUserFriendlyMessage(jobResult.ErrorMessage, jobResult.StatusCode);
            return;
        }
        Job = jobResult.Value;

        // Cargar perfil del técnico asignado (visible al cliente y admin).
        if (Job.AssignedTechnicianId.HasValue)
        {
            AssignedTechnicianId = Job.AssignedTechnicianId;
            var profileResult = await apiClient.GetTechnicianProfileAsync(Job.AssignedTechnicianId.Value);
            if (profileResult.IsSuccess && profileResult.Value != null)
                AssignedTechnicianProfile = profileResult.Value;
        }

        if (CanSeeProposals)
        {
            var propsResult = await apiClient.GetJobProposalsAsync(id);
            if (propsResult.IsSuccess)
            {
                Proposals = propsResult.Value;
                foreach (var techId in Proposals?.Select(p => p.TechnicianId).Distinct() ?? [])
                {
                    var profileResult = await apiClient.GetTechnicianProfileAsync(techId);
                    if (profileResult.IsSuccess && profileResult.Value != null)
                        TechnicianProfiles[techId] = profileResult.Value;
                }
            }
        }
    }

    // Delegados al helper centralizado para uso en la vista.
    public static string StatusLabel(string status) => StatusHelper.Label(status);
    public static string StatusBadge(string status) => StatusHelper.Badge(status);
}
