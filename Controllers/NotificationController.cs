using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AnywhereEdureach.Controllers;

[Authorize]
public class NotificationController(ApplicationDbContext db) : Controller
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET: Notification/Index
    public IActionResult Index()
    {
        // Keep old mentor-mentee links working while showing the unified feed.
        return RedirectToAction("Index", "Notifications");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarkAllAsRead()
    {
        db.BookingNotifications
            .Where(n => n.UserId == CurrentUserId && !n.IsRead)
            .ExecuteUpdate(setters => setters.SetProperty(n => n.IsRead, true));

        return Request.IsAjax() ? Json(new { success = true, reload = true }) : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult MarkAsRead(int id)
    {
        var n = db.BookingNotifications.Find(id);
        if (n == null) return RedirectToAction("Index");
        if (n.UserId != CurrentUserId) return Forbid();

        n.IsRead = true;
        db.SaveChanges();

        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Open(int id)
    {
        var n = db.BookingNotifications.FirstOrDefault(x => x.Id == id && x.UserId == CurrentUserId);
        if (n == null) return NotFound();

        n.IsRead = true;
        db.SaveChanges();
        return n.BookingId.HasValue
            ? RedirectToAction("Show", "Booking", new { id = n.BookingId.Value })
            : RedirectToAction(nameof(Index));
    }
}
