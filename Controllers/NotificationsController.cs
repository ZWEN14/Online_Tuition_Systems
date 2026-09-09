using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.ViewModels.Notifications;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.ModuleUsers)]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(ApplicationDbContext context)
    {
        _context = context;
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

        var allForUser = _context.Notifications
            .AsNoTracking()
            .Where(item => item.UserId == userId.Value);

        var query = filter == "unread"
            ? allForUser.Where(item => !item.ReadAt.HasValue)
            : allForUser;

        var model = new NotificationIndexViewModel
        {
            Filter = filter,
            Page = page,
            UnreadCount = await allForUser.CountAsync(item => !item.ReadAt.HasValue),
            TotalCount = await query.CountAsync()
        };

        model.Page = Math.Min(model.Page, model.TotalPages);
        model.Items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((model.Page - 1) * model.PageSize)
            .Take(model.PageSize)
            .ToListAsync();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(int id)
    {
        var userId = await GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Forbid();
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
