using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Online_Tuition_Systems.Services;
using Online_Tuition_Systems.ViewModels.Notifications;

namespace Online_Tuition_Systems.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly NotificationFeedService _feed;

    public NotificationBellViewComponent(NotificationFeedService feed)
    {
        _feed = feed;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userIdValue = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Content(string.Empty);
        }

        return View(new NotificationBellViewModel
        {
            UnreadCount = await _feed.CountAsync(userId, unreadOnly: true),
            RecentItems = await _feed.GetItemsAsync(userId, unreadOnly: false, skip: 0, take: 5)
        });
    }
}
