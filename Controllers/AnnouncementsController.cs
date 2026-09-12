using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.Services;
using Online_Tuition_Systems.ViewModels.Announcements;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.ModuleUsers)]
public class AnnouncementsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public AnnouncementsController(
        ApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(AnnouncementIndexViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid announcement filter values.");
        }

        await PopulateIndexViewModel(model);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> List(AnnouncementIndexViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid announcement filter values.");
        }

        await PopulateIndexViewModel(model);
        return PartialView("_AnnouncementList", model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var announcement = await VisibleAnnouncements()
            .SingleOrDefaultAsync(item => item.Id == id);

        if (announcement is null)
        {
            return NotFound();
        }

        ViewData["RelatedCourse"] = announcement.CourseId.HasValue
            ? await _context.Courses.AsNoTracking()
                .SingleOrDefaultAsync(item => item.CourseId == announcement.CourseId.Value
                    && item.Status == CourseStatus.Published)
            : null;
        return View(announcement);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create()
    {
        return View(new CreateAnnouncementViewModel
        {
            CourseOptions = await GetCourseOptionsAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(CreateAnnouncementViewModel model)
    {
        if (model.CourseId.HasValue && !await IsPublishedCourseAsync(model.CourseId.Value))
        {
            ModelState.AddModelError(nameof(model.CourseId), "Select a published course.");
        }

        if (model.ExpiresAt.HasValue && model.ExpiresAt.Value <= DateTimeOffset.Now)
        {
            ModelState.AddModelError(
                nameof(model.ExpiresAt),
                "Expiry date and time must be in the future.");
        }

        if (!ModelState.IsValid)
        {
            model.CourseOptions = await GetCourseOptionsAsync();
            return View(model);
        }

        var currentTime = DateTimeOffset.UtcNow;
        var announcement = new Announcement
        {
            CourseId = model.CourseId,
            Title = model.Title.Trim(),
            Content = model.Content.Trim(),
            Audience = model.Audience,
            Priority = model.Priority,
            Status = AnnouncementStatus.Draft,
            CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            ExpiresAt = model.ExpiresAt?.ToUniversalTime(),
            CreatedAt = currentTime,
            UpdatedAt = currentTime
        };

        _context.Announcements.Add(announcement);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Announcement was created as a draft.";

        return RedirectToAction(nameof(Details), new { id = announcement.Id });
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var announcement = await _context.Announcements
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id);

        if (announcement is null)
        {
            return NotFound();
        }

        if (announcement.Status == AnnouncementStatus.Archived)
        {
            TempData["ErrorMessage"] = "Archived announcements cannot be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new EditAnnouncementViewModel
        {
            Id = announcement.Id,
            CourseId = announcement.CourseId,
            CourseOptions = await GetCourseOptionsAsync(announcement.CourseId),
            Title = announcement.Title,
            Content = announcement.Content,
            Audience = announcement.Audience,
            Priority = announcement.Priority,
            ExpiresAt = announcement.ExpiresAt?.ToLocalTime()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id, EditAnnouncementViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (model.ExpiresAt.HasValue && model.ExpiresAt.Value <= DateTimeOffset.Now)
        {
            ModelState.AddModelError(
                nameof(model.ExpiresAt),
                "Expiry date and time must be in the future.");
        }

        if (!ModelState.IsValid)
        {
            model.CourseOptions = await GetCourseOptionsAsync(model.CourseId);
            return View(model);
        }

        var announcement = await _context.Announcements
            .SingleOrDefaultAsync(item => item.Id == id);

        if (announcement is null)
        {
            return NotFound();
        }

        if (announcement.Status == AnnouncementStatus.Archived)
        {
            TempData["ErrorMessage"] = "Archived announcements cannot be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (model.CourseId.HasValue
            && announcement.CourseId != model.CourseId
            && !await IsPublishedCourseAsync(model.CourseId.Value))
        {
            ModelState.AddModelError(nameof(model.CourseId), "Select a published course.");
            model.CourseOptions = await GetCourseOptionsAsync(model.CourseId);
            return View(model);
        }

        if (announcement.Status == AnnouncementStatus.Published
            && announcement.CourseId != model.CourseId)
        {
            ModelState.AddModelError(nameof(model.CourseId),
                "The linked course cannot be changed after publication.");
            model.CourseOptions = await GetCourseOptionsAsync(model.CourseId);
            return View(model);
        }

        var title = model.Title.Trim();
        var content = model.Content.Trim();
        var expiresAt = model.ExpiresAt?.ToUniversalTime();
        var hasChanges = announcement.Title != title
            || announcement.CourseId != model.CourseId
            || announcement.Content != content
            || announcement.Audience != model.Audience
            || announcement.Priority != model.Priority
            || announcement.ExpiresAt != expiresAt;

        announcement.Title = title;
        announcement.CourseId = model.CourseId;
        announcement.Content = content;
        announcement.Audience = model.Audience;
        announcement.Priority = model.Priority;
        announcement.ExpiresAt = expiresAt;
        announcement.UpdatedAt = DateTimeOffset.UtcNow;

        if (hasChanges && announcement.Status == AnnouncementStatus.Published)
        {
            await _notificationService.AnnouncementUpdatedAsync(announcement);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Announcement updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Publish(int id)
    {
        var announcement = await _context.Announcements
            .SingleOrDefaultAsync(item => item.Id == id);

        if (announcement is null)
        {
            return NotFound();
        }

        if (announcement.Status != AnnouncementStatus.Draft)
        {
            TempData["ErrorMessage"] = "Only draft announcements can be published.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (announcement.CourseId.HasValue
            && !await IsPublishedCourseAsync(announcement.CourseId.Value))
        {
            TempData["ErrorMessage"] = "The linked course must still be published before this announcement can be published.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var currentTime = DateTimeOffset.UtcNow;
        announcement.Status = AnnouncementStatus.Published;
        announcement.PublishedAt = currentTime;
        announcement.UpdatedAt = currentTime;

        await _notificationService.AnnouncementPublishedAsync(announcement);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Announcement published successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Archive(int id)
    {
        var announcement = await _context.Announcements
            .SingleOrDefaultAsync(item => item.Id == id);

        if (announcement is null)
        {
            return NotFound();
        }

        if (announcement.Status != AnnouncementStatus.Published)
        {
            TempData["ErrorMessage"] = "Only published announcements can be archived.";
            return RedirectToAction(nameof(Details), new { id });
        }

        announcement.Status = AnnouncementStatus.Archived;
        announcement.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Announcement archived successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private IQueryable<Announcement> VisibleAnnouncements()
    {
        var query = _context.Announcements.AsNoTracking();

        if (User.IsInRole(AppRoles.Admin))
        {
            return query;
        }

        var currentTime = DateTimeOffset.UtcNow;

        if (User.IsInRole(AppRoles.Student))
        {
            return query.Where(item =>
                item.Status == AnnouncementStatus.Published
                && item.PublishedAt.HasValue
                && item.PublishedAt.Value <= currentTime
                && (!item.ExpiresAt.HasValue || item.ExpiresAt.Value > currentTime)
                && (item.Audience == AnnouncementAudience.All
                    || item.Audience == AnnouncementAudience.Student));
        }

        if (User.IsInRole(AppRoles.Tutor))
        {
            return query.Where(item =>
                item.Status == AnnouncementStatus.Published
                && item.PublishedAt.HasValue
                && item.PublishedAt.Value <= currentTime
                && (!item.ExpiresAt.HasValue || item.ExpiresAt.Value > currentTime)
                && (item.Audience == AnnouncementAudience.All
                    || item.Audience == AnnouncementAudience.Tutor));
        }

        return query.Where(item => false);
    }

    private async Task PopulateIndexViewModel(AnnouncementIndexViewModel model)
    {
        model.Search = string.IsNullOrWhiteSpace(model.Search)
            ? null
            : model.Search.Trim();
        model.PageSize = 8;

        var query = VisibleAnnouncements();

        if (model.Search is not null)
        {
            query = query.Where(item =>
                item.Title.Contains(model.Search)
                || item.Content.Contains(model.Search));
        }

        if (model.Audience.HasValue)
        {
            query = query.Where(item => item.Audience == model.Audience.Value);
        }

        if (model.Priority.HasValue)
        {
            query = query.Where(item => item.Priority == model.Priority.Value);
        }

        if (model.Status.HasValue)
        {
            query = query.Where(item => item.Status == model.Status.Value);
        }

        query = model.Sort switch
        {
            "oldest" => query.OrderBy(item => item.CreatedAt),
            "title" => query.OrderBy(item => item.Title),
            _ => query.OrderByDescending(item => item.CreatedAt)
        };

        model.TotalCount = await query.CountAsync();
        model.Page = Math.Min(model.Page, model.TotalPages);
        model.Items = await query
            .Skip((model.Page - 1) * model.PageSize)
            .Take(model.PageSize)
            .ToListAsync();
    }

    private Task<bool> IsPublishedCourseAsync(int courseId) =>
        _context.Courses.AnyAsync(item =>
            item.CourseId == courseId && item.Status == CourseStatus.Published);

    private async Task<List<SelectListItem>> GetCourseOptionsAsync(int? currentCourseId = null)
    {
        var courses = await _context.Courses.AsNoTracking()
            .Where(item => item.Status == CourseStatus.Published
                || item.CourseId == currentCourseId)
            .OrderBy(item => item.Title)
            .Select(item => new { item.CourseId, item.Code, item.Title, item.Status })
            .ToListAsync();

        return courses.Select(item => new SelectListItem
        {
            Value = item.CourseId.ToString(),
            Text = item.Status == CourseStatus.Published
                ? $"{item.Code} — {item.Title}"
                : $"{item.Code} — {item.Title} ({item.Status})"
        }).ToList();
    }
}
