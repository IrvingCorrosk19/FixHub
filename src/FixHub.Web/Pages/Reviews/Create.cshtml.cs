using System.ComponentModel.DataAnnotations;
using FixHub.Web.Helpers;
using FixHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages.Reviews;

[Authorize]
public class CreateModel(IFixHubApiClient apiClient) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        public Guid JobId { get; set; }

        [Required]
        public Guid RevieweeId { get; set; }

        [Required, Range(1, 5, ErrorMessage = "Selecciona una calificación entre 1 y 5.")]
        public int Stars { get; set; } = 5;

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }

    public void OnGet(Guid jobId, Guid technicianId)
    {
        Input.JobId = jobId;
        Input.RevieweeId = technicianId;
        Input.Stars = 5;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = await apiClient.CreateReviewAsync(
            new CreateReviewRequest(Input.JobId, Input.RevieweeId, Input.Stars, Input.Comment));

        if (!result.IsSuccess)
        {
            ErrorMessage = ErrorMessageHelper.GetUserFriendlyMessage(result.ErrorMessage, result.StatusCode);
            return Page();
        }

        TempData["Success"] = "Reseña publicada correctamente.";
        return RedirectToPage("/Jobs/Detail", new { id = Input.JobId });
    }
}
