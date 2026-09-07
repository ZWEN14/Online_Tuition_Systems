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
        var email = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(email))
        {
            return Content(string.Empty);
        }

        var userId = await _context.Users
            .AsNoTracking()
            .Where(item => item.Email == email)
            .Select(item => (int?)item.Id)
            .SingleOrDefaultAsync();

        if (!userId.HasValue)
        {
            return Content(string.Empty);
        }

        var query = _context.Notifications
            .AsNoTracking()
            .Where(item => item.UserId == userId.Value);

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
