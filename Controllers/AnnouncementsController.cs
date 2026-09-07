using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.ViewModels.Announcements;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.ModuleUsers)]
public class AnnouncementsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AnnouncementsController(ApplicationDbContext context)
    {
        _context = context;
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

        return View(announcement);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult Create()
    {
        return View(new CreateAnnouncementViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(CreateAnnouncementViewModel model)
    {
        if (model.ExpiresAt.HasValue && model.ExpiresAt.Value <= DateTimeOffset.Now)
        {
            ModelState.AddModelError(
                nameof(model.ExpiresAt),
                "Expiry date and time must be in the future.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var currentTime = DateTimeOffset.UtcNow;
        var announcement = new Announcement
        {
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

        announcement.Title = model.Title.Trim();
        announcement.Content = model.Content.Trim();
        announcement.Audience = model.Audience;
        announcement.Priority = model.Priority;
        announcement.ExpiresAt = model.ExpiresAt?.ToUniversalTime();
        announcement.UpdatedAt = DateTimeOffset.UtcNow;

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

        var currentTime = DateTimeOffset.UtcNow;
        announcement.Status = AnnouncementStatus.Published;
        announcement.PublishedAt = currentTime;
        announcement.UpdatedAt = currentTime;

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
}
