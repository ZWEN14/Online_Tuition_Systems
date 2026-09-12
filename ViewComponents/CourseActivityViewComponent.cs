using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.ViewComponents;

public sealed class CourseActivityViewComponent(ApplicationDbContext context) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int courseId)
    {
        var user = HttpContext.User;
        if (user.Identity?.IsAuthenticated != true
            || (!user.IsInRole(AppRoles.Admin)
                && !user.IsInRole(AppRoles.Student)
                && !user.IsInRole(AppRoles.Tutor)))
        {
            return Content(string.Empty);
        }

        var now = DateTimeOffset.UtcNow;
        var events = context.Events.AsNoTracking()
            .Where(item => item.CourseId == courseId
                && item.Status == EventStatus.Published
                && item.StartsAt > now);
        var announcements = context.Announcements.AsNoTracking()
            .Where(item => item.CourseId == courseId
                && item.Status == AnnouncementStatus.Published
                && item.PublishedAt.HasValue
                && item.PublishedAt.Value <= now
                && (!item.ExpiresAt.HasValue || item.ExpiresAt.Value > now));

        if (user.IsInRole(AppRoles.Student))
        {
            events = events.Where(item => item.RegistrationAudience == RegistrationAudience.All
                || item.RegistrationAudience == RegistrationAudience.Student);
            announcements = announcements.Where(item => item.Audience == AnnouncementAudience.All
                || item.Audience == AnnouncementAudience.Student);
        }
        else if (user.IsInRole(AppRoles.Tutor))
        {
            events = events.Where(item => item.RegistrationAudience == RegistrationAudience.All
                || item.RegistrationAudience == RegistrationAudience.Tutor);
            announcements = announcements.Where(item => item.Audience == AnnouncementAudience.All
                || item.Audience == AnnouncementAudience.Tutor);
        }

        return View(new CourseActivityViewModel
        {
            UpcomingEvents = await events.OrderBy(item => item.StartsAt).Take(4).ToListAsync(),
            Announcements = await announcements.OrderByDescending(item => item.PublishedAt).Take(4).ToListAsync()
        });
    }
}
