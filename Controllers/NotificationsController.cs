using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Services;
using Online_Tuition_Systems.ViewModels.Notifications;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.ModuleUsers)]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationFeedService _feed;

    public NotificationsController(ApplicationDbContext context, NotificationFeedService feed)
    {
        _context = context;
        _feed = feed;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string filter = "all", int page = 1)
    {
        var userId = await GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Forbid();
        }

        filter = string.Equals(filter, "unread", StringComparison.OrdinalIgnoreCase)
            ? "unread"
            : "all";
        page = Math.Max(1, page);

        var model = new NotificationIndexViewModel
        {
            Filter = filter,
            Page = page,
            UnreadCount = await _feed.CountAsync(userId.Value, unreadOnly: true),
            TotalCount = await _feed.CountAsync(userId.Value, unreadOnly: filter == "unread")
        };

        model.Page = Math.Min(model.Page, model.TotalPages);
        model.Items = await _feed.GetItemsAsync(
            userId.Value,
            unreadOnly: filter == "unread",
            skip: (model.Page - 1) * model.PageSize,
            take: model.PageSize);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(int id, string source = "module")
    {
        var userId = await GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Forbid();
        }

        if (source == "booking")
        {
            var bookingNotification = await _context.BookingNotifications
                .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId.Value);

            if (bookingNotification is null)
            {
                return NotFound();
            }

            if (!bookingNotification.IsRead)
            {
                bookingNotification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return bookingNotification.BookingId.HasValue
                ? RedirectToAction("Show", "Booking", new { id = bookingNotification.BookingId.Value })
                : RedirectToAction(nameof(Index));
        }

        if (source != "module")
        {
            return BadRequest();
        }

        var notification = await _context.Notifications
            .SingleOrDefaultAsync(item =>
                item.Id == id
                && item.UserId == userId.Value);

        if (notification is null)
        {
            return NotFound();
        }

        if (!notification.ReadAt.HasValue)
        {
            notification.ReadAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }

        return !string.IsNullOrWhiteSpace(notification.TargetUrl)
            && Url.IsLocalUrl(notification.TargetUrl)
                ? LocalRedirect(notification.TargetUrl)
                : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = await GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Forbid();
        }

        var currentTime = DateTimeOffset.UtcNow;
        await _context.Notifications
            .Where(item =>
                item.UserId == userId.Value
                && !item.ReadAt.HasValue)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.ReadAt, currentTime));
        await _context.BookingNotifications
            .Where(item => item.UserId == userId.Value && !item.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.IsRead, true));

        TempData["SuccessMessage"] = "All notifications were marked as read.";
        return RedirectToAction(nameof(Index));
    }

    private Task<int?> GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Task.FromResult<int?>(null);
        }

        return Task.FromResult<int?>(userId);
    }
}
