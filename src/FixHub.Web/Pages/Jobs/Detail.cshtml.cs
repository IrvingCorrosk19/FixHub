using System.ComponentModel.DataAnnotations;
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
    public string? ProposalError { get; set; }
    public bool AlreadyProposed { get; set; }
    public bool HasReview { get; set; }
    public Guid? AssignedTechnicianId { get; set; }

    public bool IsCustomer => SessionUser.IsCustomer(User);
    public bool IsTechnician => SessionUser.IsTechnician(User);
    public bool IsAdmin => SessionUser.GetRole(User) == "Admin";
    public bool IsOwner => Job?.CustomerId == SessionUser.GetUserId(User);
    public bool CanSeeProposals => IsOwner || IsAdmin;

    [BindProperty]
    public ProposalInputModel ProposalInput { get; set; } = new();

    public class ProposalInputModel
    {
        [Required, Range(1, 999999)]
        public decimal Price { get; set; }

        [Required, Range(1, 365)]
        public int EstimatedDays { get; set; } = 1;

        [Required, MaxLength(1000)]
        public string CoverLetter { get; set; } = string.Empty;
    }

    public static string StatusBadge(string status) => status switch
    {
        "Open" => "bg-success",
        "Assigned" => "bg-primary",
        "InProgress" => "bg-warning text-dark",
        "Completed" => "bg-secondary",
        _ => "bg-light text-dark"
    };

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        await LoadJobAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostSubmitProposalAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            await LoadJobAsync(id);
            return Page();
        }

        var result = await apiClient.SubmitProposalAsync(id,
            new SubmitProposalRequest(ProposalInput.Price, ProposalInput.CoverLetter));

        if (!result.IsSuccess)
        {
            ProposalError = result.ErrorMessage ?? "Error al enviar la propuesta.";
            await LoadJobAsync(id);
            return Page();
        }

        TempData["Success"] = "Propuesta enviada correctamente.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid id)
    {
        var result = await apiClient.CompleteJobAsync(id);

        if (!result.IsSuccess)
            TempData["Error"] = result.ErrorMessage ?? "Error al completar el trabajo.";
        else
            TempData["Success"] = "Trabajo marcado como completado.";

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAcceptAsync(Guid id, Guid proposalId)
    {
        var result = await apiClient.AcceptProposalAsync(proposalId);

        if (!result.IsSuccess)
            TempData["Error"] = result.ErrorMessage ?? "Error al aceptar la propuesta.";
        else
            TempData["Success"] = $"Propuesta de {result.Value?.TechnicianName} aceptada.";

        return RedirectToPage(new { id });
    }

    private async Task LoadJobAsync(Guid id)
    {
        var jobResult = await apiClient.GetJobAsync(id);
        if (!jobResult.IsSuccess) return;
        Job = jobResult.Value;

        if (CanSeeProposals)
        {
            var propsResult = await apiClient.GetJobProposalsAsync(id);
            if (propsResult.IsSuccess)
            {
                Proposals = propsResult.Value;
                var currentUserId = SessionUser.GetUserId(User);
                AlreadyProposed = Proposals?.Any(p => p.TechnicianId == currentUserId) ?? false;

                var accepted = Proposals?.FirstOrDefault(p => p.Status == "Accepted");
                AssignedTechnicianId = accepted?.TechnicianId;
                HasReview = false;
            }

            if (AssignedTechnicianId.HasValue)
            {
                var profileResult = await apiClient.GetTechnicianProfileAsync(AssignedTechnicianId.Value);
                if (profileResult.IsSuccess && profileResult.Value != null)
                    AssignedTechnicianProfile = profileResult.Value;
            }

            foreach (var techId in Proposals?.Select(p => p.TechnicianId).Distinct() ?? [])
            {
                var profileResult = await apiClient.GetTechnicianProfileAsync(techId);
                if (profileResult.IsSuccess && profileResult.Value != null)
                    TechnicianProfiles[techId] = profileResult.Value;
            }
        }
        else if (IsTechnician)
        {
            // Technician checks if they already proposed via their own assignments
            var propsResult = await apiClient.GetJobProposalsAsync(id);
            if (propsResult.IsSuccess)
            {
                var currentUserId = SessionUser.GetUserId(User);
                AlreadyProposed = propsResult.Value?.Any(p => p.TechnicianId == currentUserId) ?? false;
                Proposals = propsResult.Value?.Where(p => p.TechnicianId == currentUserId).ToList();
            }
        }
    }
}
