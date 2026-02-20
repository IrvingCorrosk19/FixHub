using FixHub.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FixHub.Web.Pages.Notifications;

[Authorize]
public class IndexModel(IFixHubApiClient api) : PageModel
{
    public PagedResult<NotificationDto> Notifications { get; set; } = null!;
    public int PageNum { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync(int page = 1, Guid? markRead = null, string? redirect = null, CancellationToken ct = default)
    {
        if (markRead.HasValue)
        {
            await api.MarkNotificationReadAsync(markRead.Value);
            if (!string.IsNullOrEmpty(redirect) && redirect.StartsWith("/"))
                return Redirect(redirect);
            return RedirectToPage("/Notifications/Index", new { page });
        }

        PageNum = page < 1 ? 1 : page;
        var result = await api.GetNotificationsAsync(PageNum, 20);
        if (!result.IsSuccess)
        {
            Notifications = new PagedResult<NotificationDto>(new List<NotificationDto>(), 0, 1, 20, 0, false, false);
            return Page();
        }

        Notifications = result.Value!;
        return Page();
    }
}
