using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.Services;
using Online_Tuition_Systems.ViewModels.EventRegistrations;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.ModuleUsers)]
public class EventRegistrationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public EventRegistrationsController(
        ApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.StudentOrTutor)]
    public async Task<IActionResult> Register(int eventId)
    {
        var tuitionEvent = await _context.Events
            .AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == eventId
                && item.Status == EventStatus.Published);

        if (tuitionEvent is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();

        if (user is null)
        {
            return Forbid();
        }

        var existingRegistration = await _context.EventRegistrations
            .AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.EventId == eventId && item.UserId == user.Id);

        var unavailableReason = await GetRegistrationUnavailableReason(
            tuitionEvent,
            user,
            existingRegistration);

        if (unavailableReason is not null)
        {
            TempData["ErrorMessage"] = unavailableReason;
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        return View(new RegisterEventViewModel
        {
            EventId = tuitionEvent.Id,
            Event = tuitionEvent,
            Message = existingRegistration?.Message
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.StudentOrTutor)]
    public async Task<IActionResult> Register(RegisterEventViewModel model)
    {
        var tuitionEvent = await _context.Events
            .SingleOrDefaultAsync(item =>
                item.Id == model.EventId
                && item.Status == EventStatus.Published);

        if (tuitionEvent is null)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();

        if (user is null)
        {
            return Forbid();
        }

        var existingRegistration = await _context.EventRegistrations
            .SingleOrDefaultAsync(item =>
                item.EventId == model.EventId && item.UserId == user.Id);

        var unavailableReason = await GetRegistrationUnavailableReason(
            tuitionEvent,
            user,
            existingRegistration);

        if (unavailableReason is not null)
        {
            ModelState.AddModelError(string.Empty, unavailableReason);
        }

        if (!ModelState.IsValid)
        {
            model.Event = tuitionEvent;
            return View(model);
        }

        var currentTime = DateTimeOffset.UtcNow;

        EventRegistration registration;
        var isNewRegistration = existingRegistration is null;

        if (isNewRegistration)
        {
            registration = new EventRegistration
            {
                EventId = tuitionEvent.Id,
                Event = tuitionEvent,
                UserId = user.Id,
                User = user,
                Message = CleanOptionalText(model.Message),
                Status = EventRegistrationStatus.Pending,
                CreatedAt = currentTime,
                UpdatedAt = currentTime
            };

            _context.EventRegistrations.Add(registration);
        }
        else
        {
            registration = existingRegistration!;
            registration.Event = tuitionEvent;
            registration.User = user;
            registration.Message = CleanOptionalText(model.Message);
            registration.Status = EventRegistrationStatus.Pending;
            registration.ReviewedByUserId = null;
            registration.ReviewedAt = null;
            registration.ReviewNote = null;
            registration.UpdatedAt = currentTime;
        }

        try
        {
            if (isNewRegistration)
            {
                await _notificationService.RegistrationSubmittedAsync(registration);
            }
            else
            {
                await _notificationService.RegistrationUpdatedAsync(registration);
            }

            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "You have already registered for this event.";
            return RedirectToAction("Details", "Events", new { id = model.EventId });
        }

        TempData["SuccessMessage"] = "Your event registration was submitted to the Event Organizer.";
        return RedirectToAction(nameof(MyRegistrations));
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.StudentOrTutor)]
    public async Task<IActionResult> MyRegistrations()
    {
        var user = await GetCurrentUserAsync();

        if (user is null)
        {
            return Forbid();
        }

        var registrations = await _context.EventRegistrations
            .AsNoTracking()
            .Include(item => item.Event)
            .Where(item => item.UserId == user.Id)
            .OrderBy(item => item.Event.StartsAt)
            .ToListAsync();

        return View(registrations);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.StudentOrTutor)]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await GetCurrentUserAsync();

        if (user is null)
        {
            return Forbid();
        }

        var registration = await _context.EventRegistrations
            .AsNoTracking()
            .Include(item => item.Event)
            .SingleOrDefaultAsync(item =>
                item.Id == id
                && item.UserId == user.Id);

        if (registration is null)
        {
            return NotFound();
        }

        if (!CanEditRegistration(registration))
        {
            TempData["ErrorMessage"] =
                "Only your pending, rejected or cancelled registration can be edited.";
            return RedirectToAction(nameof(MyRegistrations));
        }

        var unavailableReason = await GetRegistrationUnavailableReason(
            registration.Event,
            user,
            registration,
            allowPendingRegistration: true);

        if (unavailableReason is not null)
        {
            TempData["ErrorMessage"] = unavailableReason;
            return RedirectToAction(nameof(MyRegistrations));
        }

        return View(new EditEventRegistrationViewModel
        {
            Id = registration.Id,
            EventId = registration.EventId,
            Message = registration.Message,
            Event = registration.Event
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.StudentOrTutor)]
    public async Task<IActionResult> Edit(
        int id,
        EditEventRegistrationViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var user = await GetCurrentUserAsync();

        if (user is null)
        {
            return Forbid();
        }

        var registration = await _context.EventRegistrations
            .Include(item => item.Event)
            .Include(item => item.User)
            .SingleOrDefaultAsync(item =>
                item.Id == id
                && item.UserId == user.Id);

        if (registration is null || registration.EventId != model.EventId)
        {
            return NotFound();
        }

        if (!CanEditRegistration(registration))
        {
            TempData["ErrorMessage"] =
                "Only your pending, rejected or cancelled registration can be edited.";
            return RedirectToAction(nameof(MyRegistrations));
        }

        var unavailableReason = await GetRegistrationUnavailableReason(
            registration.Event,
            user,
            registration,
            allowPendingRegistration: true);

        if (unavailableReason is not null)
        {
            ModelState.AddModelError(string.Empty, unavailableReason);
        }

        if (!ModelState.IsValid)
        {
            model.Event = registration.Event;
            return View(model);
        }

        registration.Message = CleanOptionalText(model.Message);
        registration.Status = EventRegistrationStatus.Pending;
        registration.ReviewedByUserId = null;
        registration.ReviewedAt = null;
        registration.ReviewNote = null;
        registration.UpdatedAt = DateTimeOffset.UtcNow;

        await _notificationService.RegistrationUpdatedAsync(registration);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "Your registration was updated and resubmitted to the Event Organizer.";
        return RedirectToAction(nameof(MyRegistrations));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.StudentOrTutor)]
    public async Task<IActionResult> Cancel(int id)
    {
        var user = await GetCurrentUserAsync();

        if (user is null)
        {
            return Forbid();
        }

        var registration = await _context.EventRegistrations
            .Include(item => item.Event)
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == user.Id);

        if (registration is null)
        {
            return NotFound();
        }

        if (registration.Status is not (
            EventRegistrationStatus.Pending or EventRegistrationStatus.Approved))
        {
            TempData["ErrorMessage"] = "Only pending or approved registrations can be cancelled.";
            return RedirectToAction(nameof(MyRegistrations));
        }

        if (registration.Event.StartsAt <= DateTimeOffset.UtcNow)
        {
            TempData["ErrorMessage"] = "A registration cannot be cancelled after the event has started.";
            return RedirectToAction(nameof(MyRegistrations));
        }

        registration.Status = EventRegistrationStatus.Cancelled;
        registration.UpdatedAt = DateTimeOffset.UtcNow;
        await _notificationService.RegistrationCancelledAsync(registration);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your event registration was cancelled.";
        return RedirectToAction(nameof(MyRegistrations));
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Tutor)]
    public async Task<IActionResult> Manage(int? eventId)
    {
        var organizerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(organizerUserId))
        {
            return Forbid();
        }

        var query = _context.EventRegistrations
            .AsNoTracking()
            .Include(item => item.Event)
            .Include(item => item.User)
            .Where(item => item.Event.OrganizerUserId == organizerUserId)
            .AsQueryable();

        if (eventId.HasValue)
        {
            query = query.Where(item => item.EventId == eventId.Value);
        }

        var registrations = await query
            .OrderBy(item => item.Event.StartsAt)
            .ThenByDescending(item => item.CreatedAt)
            .ToListAsync();

        ViewData["EventId"] = eventId;
        return View(registrations);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Tutor)]
    public async Task<IActionResult> Approve(int id)
    {
        var organizer = await GetCurrentUserAsync();

        if (organizer is null)
        {
            return Forbid();
        }

        var executionStrategy = _context.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync<IActionResult>(async () =>
        {
            // A retry must start with a clean tracker and a new transaction.
            _context.ChangeTracker.Clear();

            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            var registration = await _context.EventRegistrations
                .Include(item => item.Event)
                .Include(item => item.User)
                .SingleOrDefaultAsync(item => item.Id == id);

            if (registration is null)
            {
                return NotFound();
            }

            if (registration.Event.OrganizerUserId != organizer.Id.ToString())
            {
                return Forbid();
            }

            var errorMessage = await GetApprovalError(registration);

            if (errorMessage is not null)
            {
                TempData["ErrorMessage"] = errorMessage;
                return RedirectToAction(nameof(Manage), new { eventId = registration.EventId });
            }

            registration.Status = EventRegistrationStatus.Approved;
            registration.ReviewedByUserId = organizer.Id;
            registration.ReviewedAt = DateTimeOffset.UtcNow;
            registration.ReviewNote = null;
            registration.UpdatedAt = DateTimeOffset.UtcNow;

            await _notificationService.RegistrationReviewedAsync(registration);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "The event registration was approved.";
            return RedirectToAction(nameof(Manage), new { eventId = registration.EventId });
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Tutor)]
    public async Task<IActionResult> Reject(RejectEventRegistrationViewModel model)
    {
        var registration = await _context.EventRegistrations
            .Include(item => item.Event)
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.Id == model.Id);

        if (registration is null)
        {
            return NotFound();
        }

        var organizer = await GetCurrentUserAsync();

        if (organizer is null)
        {
            return Forbid();
        }

        if (registration.Event.OrganizerUserId != organizer.Id.ToString())
        {
            return Forbid();
        }

        if (registration.Status != EventRegistrationStatus.Pending)
        {
            TempData["ErrorMessage"] = "Only pending registrations can be rejected.";
            return RedirectToAction(nameof(Manage), new { eventId = registration.EventId });
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = ModelState.Values
                .SelectMany(item => item.Errors)
                .Select(item => item.ErrorMessage)
                .FirstOrDefault() ?? "A valid rejection reason is required.";

            return RedirectToAction(nameof(Manage), new { eventId = registration.EventId });
        }

        registration.Status = EventRegistrationStatus.Rejected;
        registration.ReviewedByUserId = organizer.Id;
        registration.ReviewedAt = DateTimeOffset.UtcNow;
        registration.ReviewNote = model.ReviewNote.Trim();
        registration.UpdatedAt = DateTimeOffset.UtcNow;

        await _notificationService.RegistrationReviewedAsync(registration);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "The event registration was rejected.";
        return RedirectToAction(nameof(Manage), new { eventId = registration.EventId });
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await _context.Users.SingleOrDefaultAsync(item => item.Id == userId);
    }

    private async Task<string?> GetRegistrationUnavailableReason(
        Event tuitionEvent,
        User user,
        EventRegistration? existingRegistration,
        bool allowPendingRegistration = false)
    {
        if (tuitionEvent.Status != EventStatus.Published)
        {
            return "Registration is only available for published events.";
        }

        if (tuitionEvent.OrganizerUserId == user.Id.ToString())
        {
            return "The Event Organizer cannot register for their own event.";
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

        if (!AudienceAllows(tuitionEvent, user.Role))
        {
            return "This event is not open to your user role.";
        }

        if (existingRegistration is not null
            && existingRegistration.Status != EventRegistrationStatus.Rejected
            && existingRegistration.Status != EventRegistrationStatus.Cancelled
            && !(allowPendingRegistration
                && existingRegistration.Status == EventRegistrationStatus.Pending))
        {
            return "You have already registered for this event.";
        }

        var approvedCount = await _context.EventRegistrations.CountAsync(item =>
            item.EventId == tuitionEvent.Id
            && item.Status == EventRegistrationStatus.Approved);

        return approvedCount >= tuitionEvent.MaxParticipants
            ? "This event has reached its maximum number of participants."
            : null;
    }

    private async Task<string?> GetApprovalError(EventRegistration registration)
    {
        if (registration.Status != EventRegistrationStatus.Pending)
        {
            return "Only pending registrations can be approved.";
        }

        if (registration.Event.Status != EventStatus.Published)
        {
            return "Registrations can only be approved for a published event.";
        }

        if (registration.Event.StartsAt <= DateTimeOffset.UtcNow)
        {
            return "Registrations cannot be approved after the event has started.";
        }

        if (!AudienceAllows(registration.Event, registration.User.Role))
        {
            return "The applicant's role does not match the event audience.";
        }

        var approvedCount = await _context.EventRegistrations.CountAsync(item =>
            item.EventId == registration.EventId
            && item.Status == EventRegistrationStatus.Approved);

        return approvedCount >= registration.Event.MaxParticipants
            ? "This event has reached its maximum number of participants."
            : null;
    }

    private static bool AudienceAllows(Event tuitionEvent, UserRole role)
    {
        return tuitionEvent.RegistrationAudience == RegistrationAudience.All
            || tuitionEvent.RegistrationAudience.ToString()
                .Equals(role.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanEditRegistration(EventRegistration registration)
    {
        return registration.Status is EventRegistrationStatus.Pending
            or EventRegistrationStatus.Rejected
            or EventRegistrationStatus.Cancelled;
    }

    private static string? CleanOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
