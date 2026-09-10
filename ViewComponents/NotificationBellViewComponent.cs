using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.ViewModels.Notifications;

namespace Online_Tuition_Systems.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public NotificationBellViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userIdValue = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Content(string.Empty);
        }

        var query = _context.Notifications
            .AsNoTracking()
            .Where(item => item.UserId == userId);

        return View(new NotificationBellViewModel
        {
            UnreadCount = await query.CountAsync(item => !item.ReadAt.HasValue),
            RecentItems = await query
                .OrderByDescending(item => item.CreatedAt)
                .Take(5)
                .ToListAsync()
        });
    }
}
