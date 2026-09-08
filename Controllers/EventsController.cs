using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.Services;
using Online_Tuition_Systems.ViewModels.Events;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.ModuleUsers)]
public class EventsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public EventsController(
        ApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
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
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isOrganizer = User.IsInRole(AppRoles.Tutor)
            && !string.IsNullOrWhiteSpace(currentUserId)
            && tuitionEvent.OrganizerUserId == currentUserId;

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
                || isOrganizer
                || currentRegistration?.Status == EventRegistrationStatus.Approved,
            IsOrganizer = isOrganizer,
            RegistrationUnavailableReason = unavailableReason,
            Cancellation = new CancelEventViewModel
            {
                Id = tuitionEvent.Id
            }
        });
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Tutor)]
    public async Task<IActionResult> Edit(int id)
    {
        var organizerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(organizerUserId))
        {
            return Forbid();
        }

        var tuitionEvent = await _context.Events
            .AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == id
                && item.OrganizerUserId == organizerUserId);

        if (tuitionEvent is null)
        {
            return NotFound();
        }

        if (tuitionEvent.Status is EventStatus.Cancelled or EventStatus.Archived)
        {
            TempData["ErrorMessage"] = "Cancelled or archived events cannot be edited.";
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
    [Authorize(Roles = AppRoles.Tutor)]
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

        var organizerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(organizerUserId))
        {
            return Forbid();
        }

        var tuitionEvent = await _context.Events
            .SingleOrDefaultAsync(item =>
                item.Id == id
                && item.OrganizerUserId == organizerUserId);

        if (tuitionEvent is null)
        {
            return NotFound();
        }

        if (tuitionEvent.Status is EventStatus.Cancelled or EventStatus.Archived)
        {
            TempData["ErrorMessage"] = "Cancelled or archived events cannot be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var approvedRegistrationCount = await _context.EventRegistrations
            .CountAsync(item =>
                item.EventId == tuitionEvent.Id
                && item.Status == EventRegistrationStatus.Approved);

        if (model.MaxParticipants < approvedRegistrationCount)
        {
            ModelState.AddModelError(
                nameof(model.MaxParticipants),
                $"Capacity cannot be lower than the {approvedRegistrationCount} approved registrations.");
            return View(model);
        }

        var title = model.Title.Trim();
        var description = model.Description.Trim();
        var startsAt = model.StartsAt.ToUniversalTime();
        var endsAt = model.EndsAt.ToUniversalTime();
        var applicationDeadline = model.ApplicationDeadline?.ToUniversalTime();
        var location = model.Mode == EventMode.Online
            ? null
            : CleanOptionalText(model.Location);
        var meetingPlatform = model.Mode == EventMode.Physical
            ? null
            : model.MeetingPlatform;
        var meetingUrl = model.Mode == EventMode.Physical
            ? null
            : CleanOptionalText(model.MeetingUrl);
        var hasChanges = tuitionEvent.CourseId != model.CourseId
            || tuitionEvent.Title != title
            || tuitionEvent.Description != description
            || tuitionEvent.StartsAt != startsAt
            || tuitionEvent.EndsAt != endsAt
            || tuitionEvent.ApplicationDeadline != applicationDeadline
            || tuitionEvent.RegistrationAudience != model.RegistrationAudience
            || tuitionEvent.Mode != model.Mode
            || tuitionEvent.Location != location
            || tuitionEvent.MeetingPlatform != meetingPlatform
            || tuitionEvent.MeetingUrl != meetingUrl
            || tuitionEvent.MaxParticipants != model.MaxParticipants;

        tuitionEvent.CourseId = model.CourseId;
        tuitionEvent.Title = title;
        tuitionEvent.Description = description;
        tuitionEvent.StartsAt = startsAt;
        tuitionEvent.EndsAt = endsAt;
        tuitionEvent.ApplicationDeadline = applicationDeadline;
        tuitionEvent.RegistrationAudience = model.RegistrationAudience;
        tuitionEvent.Mode = model.Mode;
        tuitionEvent.Location = location;
        tuitionEvent.MeetingPlatform = meetingPlatform;
        tuitionEvent.MeetingUrl = meetingUrl;
        tuitionEvent.MaxParticipants = model.MaxParticipants;
        tuitionEvent.UpdatedAt = DateTimeOffset.UtcNow;

        if (hasChanges && tuitionEvent.Status == EventStatus.Published)
        {
            await _notificationService.EventUpdatedAsync(tuitionEvent);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Event updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Tutor)]
    public async Task<IActionResult> Publish(int id)
    {
        var organizerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(organizerUserId))
        {
            return Forbid();
        }

        var tuitionEvent = await _context.Events
            .SingleOrDefaultAsync(item =>
                item.Id == id
                && item.OrganizerUserId == organizerUserId);

        if (tuitionEvent is null)
        {
            return NotFound();
        }

        if (tuitionEvent.Status != EventStatus.Draft)
        {
            TempData["ErrorMessage"] = "Only draft events can be published.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var publishError = GetPublishError(tuitionEvent);

        if (publishError is not null)
        {
            TempData["ErrorMessage"] = publishError;
            return RedirectToAction(nameof(Details), new { id });
        }

        tuitionEvent.Status = EventStatus.Published;
        tuitionEvent.UpdatedAt = DateTimeOffset.UtcNow;
        await _notificationService.EventPublishedAsync(tuitionEvent);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Event published successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.AdminOrTutor)]
    public async Task<IActionResult> Cancel(
        int id,
        [Bind(Prefix = "Cancellation")] CancelEventViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = ModelState
                .SelectMany(item => item.Value?.Errors ?? [])
                .Select(item => item.ErrorMessage)
                .FirstOrDefault()
                ?? "Enter a valid cancellation reason.";

            return RedirectToAction(nameof(Details), new { id });
        }

        var tuitionEvent = await _context.Events
            .Include(item => item.SourceProposal)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (tuitionEvent is null)
        {
            return NotFound();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole(AppRoles.Admin);
        var isOrganizer = User.IsInRole(AppRoles.Tutor)
            && tuitionEvent.OrganizerUserId == currentUserId;

        if (!isAdmin && !isOrganizer)
        {
            return Forbid();
        }

        if (tuitionEvent.Status != EventStatus.Published)
        {
            TempData["ErrorMessage"] = "Only published events can be cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var currentTime = DateTimeOffset.UtcNow;

        if (tuitionEvent.StartsAt <= currentTime)
        {
            TempData["ErrorMessage"] = "An event cannot be cancelled after it has started.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Forbid();
        }

        tuitionEvent.Status = EventStatus.Cancelled;
        tuitionEvent.CancelledByUserId = currentUserId;
        tuitionEvent.CancellationReason = model.Reason.Trim();
        tuitionEvent.CancelledAt = currentTime;
        tuitionEvent.UpdatedAt = currentTime;
        await _notificationService.EventCancelledAsync(tuitionEvent);
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

        if (User.IsInRole(AppRoles.Tutor))
        {
            var organizerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return query.Where(item =>
                item.Status == EventStatus.Published
                || item.OrganizerUserId == organizerUserId);
        }

        if (User.IsInRole(AppRoles.Student))
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

    private static string? GetPublishError(Event tuitionEvent)
    {
        if (tuitionEvent.StartsAt <= DateTimeOffset.UtcNow)
        {
            return "Update the event start time before publishing.";
        }

        if (tuitionEvent.EndsAt <= tuitionEvent.StartsAt)
        {
            return "The event end must be later than its start.";
        }

        if (!tuitionEvent.ApplicationDeadline.HasValue
            || tuitionEvent.ApplicationDeadline.Value >= tuitionEvent.StartsAt)
        {
            return "Set a valid registration deadline before publishing.";
        }

        if (tuitionEvent.MaxParticipants < 1)
        {
            return "Set the maximum number of participants before publishing.";
        }

        if (tuitionEvent.Mode is EventMode.Physical or EventMode.Hybrid
            && string.IsNullOrWhiteSpace(tuitionEvent.Location))
        {
            return "Set the location before publishing this physical or hybrid event.";
        }

        if (tuitionEvent.Mode is EventMode.Online or EventMode.Hybrid
            && (!tuitionEvent.MeetingPlatform.HasValue
                || string.IsNullOrWhiteSpace(tuitionEvent.MeetingUrl)))
        {
            return "Set the meeting platform and URL before publishing this online or hybrid event.";
        }

        return null;
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
            return "Admin accounts do not register for events.";
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (User.IsInRole(AppRoles.Tutor)
            && tuitionEvent.OrganizerUserId == currentUserId)
        {
            return "The Event Organizer cannot register for their own event.";
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
