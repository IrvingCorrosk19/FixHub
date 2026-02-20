using FixHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages.Requests;

[Authorize]
public class ConfirmationModel : PageModel
{
    public Guid JobId { get; set; }

    public IActionResult OnGet(Guid id)
    {
        if (!SessionUser.IsCustomer(User))
        {
            TempData["Error"] = "Solo los clientes pueden ver esta página.";
            return RedirectToPage("/Index");
        }

        JobId = id;
        return Page();
    }
}
