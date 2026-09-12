using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.Services;
using Online_Tuition_Systems.ViewModels.Home;

namespace AnywhereEdureach.Controllers
{
    public class HomeController(ApplicationDbContext db, NotificationFeedService notifications) : Controller
    {
        // Public landing page.
        public IActionResult Welcome()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Index));
            return View();
        }

        // Logged-in dashboard.
        [Authorize(Roles = AppRoles.ModuleUsers)]
        public async Task<IActionResult> Index()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Forbid();

            var now = DateTimeOffset.UtcNow;
            var publishedCourses = db.Courses.AsNoTracking().Where(course =>
                course.Status == CourseStatus.Published
                && course.PublishedAtUtc.HasValue
                && course.PublishedAtUtc.Value <= now.UtcDateTime);
            var upcomingEvents = db.Events.AsNoTracking().Where(item =>
                item.Status == EventStatus.Published && item.StartsAt >= now);
            var visibleAnnouncements = db.Announcements.AsNoTracking().Where(item =>
                item.Status == AnnouncementStatus.Published
                && item.PublishedAt.HasValue && item.PublishedAt.Value <= now
                && (!item.ExpiresAt.HasValue || item.ExpiresAt.Value > now));

            if (User.IsInRole("Student"))
                visibleAnnouncements = visibleAnnouncements.Where(item =>
                    item.Audience == AnnouncementAudience.All || item.Audience == AnnouncementAudience.Student);
            else if (User.IsInRole("Tutor"))
                visibleAnnouncements = visibleAnnouncements.Where(item =>
                    item.Audience == AnnouncementAudience.All || item.Audience == AnnouncementAudience.Tutor);

            var model = new HomeDashboardViewModel
            {
                PublishedCourseCount = await publishedCourses.CountAsync(),
                UpcomingEventCount = await upcomingEvents.CountAsync(),
                UnreadNotificationCount = await notifications.CountAsync(userId, unreadOnly: true),
                Courses = await publishedCourses
                    .OrderByDescending(course => course.PublishedAtUtc).ThenBy(course => course.Title)
                    .Take(3)
                    .Select(course => new HomeCoursePreview
                    {
                        Id = course.CourseId, Title = course.Title,
                        Category = course.Category.Name, Price = course.Price
                    }).ToListAsync(),
                Events = await upcomingEvents.OrderBy(item => item.StartsAt).Take(3)
                    .Select(item => new HomeEventPreview
                    {
                        Id = item.Id, Title = item.Title, StartsAt = item.StartsAt
                    }).ToListAsync(),
                Announcements = await visibleAnnouncements
                    .OrderByDescending(item => item.PublishedAt).Take(3)
                    .Select(item => new HomeAnnouncementPreview
                    {
                        Id = item.Id, Title = item.Title, PublishedAt = item.PublishedAt
                    }).ToListAsync()
            };

            return View(model);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
