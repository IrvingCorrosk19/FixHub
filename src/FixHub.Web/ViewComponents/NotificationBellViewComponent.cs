using FixHub.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FixHub.Web.ViewComponents;

/// <summary>
/// Muestra la campana de notificaciones en el navbar con badge y dropdown de últimas 5.
/// </summary>
public class NotificationBellViewComponent(IFixHubApiClient api) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var countResult = await api.GetUnreadCountAsync();
        var unreadCount = countResult.IsSuccess ? countResult.Value : 0;

        var listResult = await api.GetNotificationsAsync(1, 5);
        var notifications = listResult.IsSuccess && listResult.Value?.Items != null
            ? listResult.Value.Items
            : new List<NotificationDto>();

        return View(new NotificationBellViewModel(unreadCount, notifications));
    }
}

public record NotificationBellViewModel(int UnreadCount, List<NotificationDto> RecentNotifications);
