using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.ViewModels.Events;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.ModuleUsers)]
public class EventsController : Controller
{
    private readonly ApplicationDbContext _context;

    public EventsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(EventIndexViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid event filter values.");
        }

        await PopulateIndexViewModel(model);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> List(EventIndexViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid event filter values.");
        }

        await PopulateIndexViewModel(model);
        return PartialView("_EventList", model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var tuitionEvent = await VisibleEvents()
            .Include(item => item.SourceProposal)
            .Include(item => item.Announcement)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (tuitionEvent is null)
        {
            return NotFound();
        }

        var approvedRegistrationCount = await _context.EventRegistrations
            .AsNoTracking()
            .CountAsync(item =>
                item.EventId == tuitionEvent.Id
                && item.Status == EventRegistrationStatus.Approved);

        var totalRegistrationCount = await _context.EventRegistrations
            .AsNoTracking()
            .CountAsync(item => item.EventId == tuitionEvent.Id);

        EventRegistration? currentRegistration = null;
        UserAccount? currentUser = null;

        if (!User.IsInRole(AppRoles.Admin))
        {
            currentUser = await GetCurrentUserAsync();

            if (currentUser is not null)
            {
                currentRegistration = await _context.EventRegistrations
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item =>
                        item.EventId == tuitionEvent.Id
                        && item.UserId == currentUser.Id);
            }
        }

        var unavailableReason = GetRegistrationUnavailableReason(
            tuitionEvent,
            currentUser,
            currentRegistration,
            approvedRegistrationCount);

        return View(new EventDetailsViewModel
        {
            Event = tuitionEvent,
            CurrentRegistration = currentRegistration,
            ApprovedRegistrationCount = approvedRegistrationCount,
            TotalRegistrationCount = totalRegistrationCount,
            CanRegister = unavailableReason is null,
            CanViewMeetingUrl = User.IsInRole(AppRoles.Admin)
                || currentRegistration?.Status == EventRegistrationStatus.Approved,
            RegistrationUnavailableReason = unavailableReason
        });
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var tuitionEvent = await _context.Events
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id);

        if (tuitionEvent is null)
        {
            return NotFound();
        }

        if (tuitionEvent.Status == EventStatus.Archived)
        {
            TempData["ErrorMessage"] = "Archived events cannot be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(new EditEventViewModel
        {
            Id = tuitionEvent.Id,
            CourseId = tuitionEvent.CourseId,
            Title = tuitionEvent.Title,
            Description = tuitionEvent.Description,
            StartsAt = tuitionEvent.StartsAt.ToLocalTime(),
            EndsAt = tuitionEvent.EndsAt.ToLocalTime(),
            ApplicationDeadline = tuitionEvent.ApplicationDeadline?.ToLocalTime(),
            RegistrationAudience = tuitionEvent.RegistrationAudience,
            Mode = tuitionEvent.Mode,
            Location = tuitionEvent.Location,
            MeetingPlatform = tuitionEvent.MeetingPlatform,
            MeetingUrl = tuitionEvent.MeetingUrl,
            MaxParticipants = tuitionEvent.MaxParticipants
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Edit(int id, EditEventViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var tuitionEvent = await _context.Events
            .SingleOrDefaultAsync(item => item.Id == id);

        if (tuitionEvent is null)
        {
            return NotFound();
        }

        if (tuitionEvent.Status == EventStatus.Archived)
        {
            TempData["ErrorMessage"] = "Archived events cannot be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        tuitionEvent.CourseId = model.CourseId;
        tuitionEvent.Title = model.Title.Trim();
        tuitionEvent.Description = model.Description.Trim();
        tuitionEvent.StartsAt = model.StartsAt.ToUniversalTime();
        tuitionEvent.EndsAt = model.EndsAt.ToUniversalTime();
        tuitionEvent.ApplicationDeadline = model.ApplicationDeadline?.ToUniversalTime();
        tuitionEvent.RegistrationAudience = model.RegistrationAudience;
        tuitionEvent.Mode = model.Mode;
        tuitionEvent.Location = model.Mode == EventMode.Online
            ? null
            : CleanOptionalText(model.Location);
        tuitionEvent.MeetingPlatform = model.Mode == EventMode.Physical
            ? null
            : model.MeetingPlatform;
        tuitionEvent.MeetingUrl = model.Mode == EventMode.Physical
            ? null
            : CleanOptionalText(model.MeetingUrl);
        tuitionEvent.MaxParticipants = model.MaxParticipants;
        tuitionEvent.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Event updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Publish(int id)
    {
        var tuitionEvent = await _context.Events
            .SingleOrDefaultAsync(item => item.Id == id);

        if (tuitionEvent is null)
        {
            return NotFound();
        }

        if (tuitionEvent.Status != EventStatus.Draft)
        {
            TempData["ErrorMessage"] = "Only draft events can be published.";
            return RedirectToAction(nameof(Details), new { id });
        }

        tuitionEvent.Status = EventStatus.Published;
        tuitionEvent.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Event published successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Cancel(int id)
    {
        var tuitionEvent = await _context.Events
            .SingleOrDefaultAsync(item => item.Id == id);

        if (tuitionEvent is null)
        {
            return NotFound();
        }

        if (tuitionEvent.Status != EventStatus.Published)
        {
            TempData["ErrorMessage"] = "Only published events can be cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        tuitionEvent.Status = EventStatus.Cancelled;
        tuitionEvent.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Event cancelled successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Archive(int id)
    {
        var tuitionEvent = await _context.Events
            .SingleOrDefaultAsync(item => item.Id == id);

        if (tuitionEvent is null)
        {
            return NotFound();
        }

        if (tuitionEvent.Status is not (EventStatus.Published or EventStatus.Cancelled))
        {
            TempData["ErrorMessage"] = "Only published or cancelled events can be archived.";
            return RedirectToAction(nameof(Details), new { id });
        }

        tuitionEvent.Status = EventStatus.Archived;
        tuitionEvent.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Event archived successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private IQueryable<Event> VisibleEvents()
    {
        var query = _context.Events.AsNoTracking();

        if (User.IsInRole(AppRoles.Admin))
        {
            return query;
        }

        if (User.IsInRole(AppRoles.Student) || User.IsInRole(AppRoles.Tutor))
        {
            return query.Where(item => item.Status == EventStatus.Published);
        }

        return query.Where(item => false);
    }

    private async Task PopulateIndexViewModel(EventIndexViewModel model)
    {
        model.Search = string.IsNullOrWhiteSpace(model.Search)
            ? null
            : model.Search.Trim();
        model.PageSize = 6;

        var query = VisibleEvents();

        if (model.Search is not null)
        {
            query = query.Where(item =>
                item.Title.Contains(model.Search)
                || item.Description.Contains(model.Search));
        }

        if (model.Mode.HasValue)
        {
            query = query.Where(item => item.Mode == model.Mode.Value);
        }

        if (model.Audience.HasValue)
        {
            query = query.Where(item =>
                item.RegistrationAudience == model.Audience.Value);
        }

        if (model.Status.HasValue)
        {
            query = query.Where(item => item.Status == model.Status.Value);
        }

        query = model.Sort switch
        {
            "newest" => query.OrderByDescending(item => item.CreatedAt),
            "title" => query.OrderBy(item => item.Title),
            _ => query.OrderBy(item => item.StartsAt)
        };

        model.TotalCount = await query.CountAsync();
        model.Page = Math.Min(model.Page, model.TotalPages);
        model.Items = await query
            .Skip((model.Page - 1) * model.PageSize)
            .Take(model.PageSize)
            .ToListAsync();
    }

    private static string? CleanOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task<UserAccount?> GetCurrentUserAsync()
    {
        var email = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Email == email);
    }

    private string? GetRegistrationUnavailableReason(
        Event tuitionEvent,
        UserAccount? currentUser,
        EventRegistration? currentRegistration,
        int approvedRegistrationCount)
    {
        if (User.IsInRole(AppRoles.Admin))
        {
            return "Admin accounts review registrations instead of registering.";
        }

        if (currentUser is null
            || (!User.IsInRole(AppRoles.Student) && !User.IsInRole(AppRoles.Tutor)))
        {
            return "Your account is not allowed to register for events.";
        }

        if (tuitionEvent.Status != EventStatus.Published)
        {
            return "Registration is only available for published events.";
        }

        if (tuitionEvent.StartsAt <= DateTimeOffset.UtcNow)
        {
            return "Registration is closed because the event has already started.";
        }

        if (tuitionEvent.ApplicationDeadline.HasValue
            && tuitionEvent.ApplicationDeadline.Value < DateTimeOffset.UtcNow)
        {
            return "The registration deadline has passed.";
        }

        if (tuitionEvent.RegistrationAudience != RegistrationAudience.All
            && !tuitionEvent.RegistrationAudience.ToString()
                .Equals(currentUser.Role, StringComparison.OrdinalIgnoreCase))
        {
            return "This event is not open to your user role.";
        }

        if (currentRegistration is not null
            && currentRegistration.Status is not (
                EventRegistrationStatus.Rejected or EventRegistrationStatus.Cancelled))
        {
            return "You have already registered for this event.";
        }

        return approvedRegistrationCount >= tuitionEvent.MaxParticipants
            ? "This event has reached its maximum number of participants."
            : null;
    }
}
