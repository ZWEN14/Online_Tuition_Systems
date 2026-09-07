using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.Services;
using Online_Tuition_Systems.ViewModels.EventProposals;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.ModuleUsers)]
public class EventProposalsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public EventProposalsController(
        ApplicationDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var proposals = await VisibleProposals()
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

        return View(proposals);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var proposal = await VisibleProposals()
            .SingleOrDefaultAsync(item => item.Id == id);

        if (proposal is null)
        {
            return NotFound();
        }

        return View(BuildDetailsViewModel(proposal));
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.StudentOrTutor)]
    public IActionResult Create()
    {
        return View(new EventProposalFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.StudentOrTutor)]
    public async Task<IActionResult> Create(EventProposalFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var currentTime = DateTimeOffset.UtcNow;
        var proposal = new EventProposal
        {
            ProposedByUserId = userId,
            Status = EventProposalStatus.Pending,
            CreatedAt = currentTime,
            UpdatedAt = currentTime
        };

        CopyFormToProposal(model, proposal);

        _context.EventProposals.Add(proposal);
        await _context.SaveChangesAsync();
        await _notificationService.ProposalSubmittedAsync(proposal);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Event proposal submitted for Admin review.";
        return RedirectToAction(nameof(Details), new { id = proposal.Id });
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.StudentOrTutor)]
    public async Task<IActionResult> Edit(int id)
    {
        var proposal = await FindOwnedPendingProposal(id, asNoTracking: true);

        if (proposal is null)
        {
            return NotFound();
        }

        return View(ToFormViewModel(proposal));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.StudentOrTutor)]
    public async Task<IActionResult> Edit(int id, EventProposalFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var proposal = await FindOwnedPendingProposal(id, asNoTracking: false);

        if (proposal is null)
        {
            return NotFound();
        }

        CopyFormToProposal(model, proposal);
        proposal.RevisionCount++;
        proposal.LastRevisedAt = DateTimeOffset.UtcNow;
        proposal.UpdatedAt = DateTimeOffset.UtcNow;

        await _notificationService.ProposalUpdatedAsync(proposal);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Pending proposal updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Approve(
        int id,
        [Bind(Prefix = "Approval")] ApproveEventProposalViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var proposal = await _context.EventProposals
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id);

        if (proposal is null)
        {
            return NotFound();
        }

        if (proposal.Status != EventProposalStatus.Pending)
        {
            TempData["ErrorMessage"] = "Only pending proposals can be approved.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (proposal.Mode is EventMode.Online or EventMode.Hybrid
            && string.IsNullOrWhiteSpace(model.MeetingUrl))
        {
            ModelState.AddModelError(
                "Approval.MeetingUrl",
                "Meeting URL is required for an online or hybrid event.");
        }

        if (!ModelState.IsValid)
        {
            return View(
                "Details",
                BuildDetailsViewModel(proposal, approval: model));
        }

        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return Forbid();
        }

        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var pendingProposal = await _context.EventProposals
            .SingleOrDefaultAsync(item => item.Id == id);

        if (pendingProposal is null)
        {
            return NotFound();
        }

        if (pendingProposal.Status != EventProposalStatus.Pending)
        {
            TempData["ErrorMessage"] = "This proposal has already been reviewed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var currentTime = DateTimeOffset.UtcNow;
        var tuitionEvent = new Event
        {
            CourseId = pendingProposal.CourseId,
            CreatedByUserId = adminUserId,
            Title = pendingProposal.Title,
            Description = pendingProposal.Description,
            StartsAt = model.StartsAt.ToUniversalTime(),
            EndsAt = model.EndsAt.ToUniversalTime(),
            ApplicationDeadline = model.ApplicationDeadline?.ToUniversalTime(),
            RegistrationAudience = model.RegistrationAudience,
            Mode = pendingProposal.Mode,
            Location = pendingProposal.Location,
            MeetingPlatform = pendingProposal.MeetingPlatform,
            MeetingUrl = CleanOptionalText(model.MeetingUrl),
            MaxParticipants = model.MaxParticipants,
            Status = EventStatus.Draft,
            CreatedAt = currentTime,
            UpdatedAt = currentTime
        };

        _context.Events.Add(tuitionEvent);
        await _context.SaveChangesAsync();

        Announcement? publishedAnnouncement = null;

        if (pendingProposal.PublishAsAnnouncement)
        {
            publishedAnnouncement = new Announcement
            {
                CourseId = pendingProposal.CourseId,
                EventId = tuitionEvent.Id,
                CreatedByUserId = adminUserId,
                Title = tuitionEvent.Title,
                Content = tuitionEvent.Description,
                Audience = ToAnnouncementAudience(tuitionEvent.RegistrationAudience),
                Priority = AnnouncementPriority.Normal,
                Status = AnnouncementStatus.Published,
                PublishedAt = currentTime,
                ExpiresAt = tuitionEvent.EndsAt,
                CreatedAt = currentTime,
                UpdatedAt = currentTime
            };

            _context.Announcements.Add(publishedAnnouncement);
        }

        pendingProposal.Status = EventProposalStatus.Approved;
        pendingProposal.ReviewedByUserId = adminUserId;
        pendingProposal.ReviewedAt = currentTime;
        pendingProposal.ReviewNote = null;
        pendingProposal.CreatedEventId = tuitionEvent.Id;
        pendingProposal.UpdatedAt = currentTime;

        await _context.SaveChangesAsync();
        await _notificationService.ProposalReviewedAsync(pendingProposal);

        if (publishedAnnouncement is not null)
        {
            await _notificationService.AnnouncementPublishedAsync(publishedAnnouncement);
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = "Proposal approved and a draft event was created.";
        return RedirectToAction("Details", "Events", new { id = tuitionEvent.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Reject(
        int id,
        [Bind(Prefix = "Rejection")] RejectEventProposalViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var proposal = await _context.EventProposals
            .SingleOrDefaultAsync(item => item.Id == id);

        if (proposal is null)
        {
            return NotFound();
        }

        if (proposal.Status != EventProposalStatus.Pending)
        {
            TempData["ErrorMessage"] = "Only pending proposals can be rejected.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!ModelState.IsValid)
        {
            return View(
                "Details",
                BuildDetailsViewModel(
                    proposal,
                    rejection: model));
        }

        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return Forbid();
        }

        var currentTime = DateTimeOffset.UtcNow;
        proposal.Status = EventProposalStatus.Rejected;
        proposal.ReviewedByUserId = adminUserId;
        proposal.ReviewedAt = currentTime;
        proposal.ReviewNote = model.ReviewNote.Trim();
        proposal.UpdatedAt = currentTime;

        await _notificationService.ProposalReviewedAsync(proposal);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Proposal rejected.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private IQueryable<EventProposal> VisibleProposals()
    {
        var query = _context.EventProposals
            .AsNoTracking()
            .Include(item => item.CreatedEvent);

        if (User.IsInRole(AppRoles.Admin))
        {
            return query;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if ((User.IsInRole(AppRoles.Student) || User.IsInRole(AppRoles.Tutor))
            && !string.IsNullOrWhiteSpace(userId))
        {
            return query.Where(item => item.ProposedByUserId == userId);
        }

        return query.Where(item => false);
    }

    private Task<EventProposal?> FindOwnedPendingProposal(int id, bool asNoTracking)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        IQueryable<EventProposal> query = _context.EventProposals;

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(item =>
            item.Id == id
            && item.ProposedByUserId == userId
            && item.Status == EventProposalStatus.Pending);
    }

    private static EventProposalDetailsViewModel BuildDetailsViewModel(
        EventProposal proposal,
        ApproveEventProposalViewModel? approval = null,
        RejectEventProposalViewModel? rejection = null)
    {
        var defaultStart = proposal.PreferredStartsAt?.ToLocalTime()
            ?? DateTimeOffset.Now.AddDays(7);
        var defaultEnd = proposal.PreferredEndsAt?.ToLocalTime()
            ?? defaultStart.AddHours(2);

        return new EventProposalDetailsViewModel
        {
            Proposal = proposal,
            Approval = approval ?? new ApproveEventProposalViewModel
            {
                Id = proposal.Id,
                StartsAt = defaultStart,
                EndsAt = defaultEnd,
                ApplicationDeadline = proposal.ProposedApplicationDeadline?.ToLocalTime(),
                RegistrationAudience = proposal.ProposedRegistrationAudience,
                MaxParticipants = proposal.ProposedMaxParticipants
            },
            Rejection = rejection ?? new RejectEventProposalViewModel
            {
                Id = proposal.Id
            }
        };
    }

    private static EventProposalFormViewModel ToFormViewModel(EventProposal proposal)
    {
        return new EventProposalFormViewModel
        {
            Id = proposal.Id,
            CourseId = proposal.CourseId,
            Title = proposal.Title,
            Description = proposal.Description,
            Reason = proposal.Reason,
            PreferredStartsAt = proposal.PreferredStartsAt?.ToLocalTime(),
            PreferredEndsAt = proposal.PreferredEndsAt?.ToLocalTime(),
            ProposedApplicationDeadline = proposal.ProposedApplicationDeadline?.ToLocalTime(),
            ProposedRegistrationAudience = proposal.ProposedRegistrationAudience,
            Mode = proposal.Mode,
            Location = proposal.Location,
            MeetingPlatform = proposal.MeetingPlatform,
            ProposedMaxParticipants = proposal.ProposedMaxParticipants,
            PublishAsAnnouncement = proposal.PublishAsAnnouncement
        };
    }

    private static void CopyFormToProposal(
        EventProposalFormViewModel model,
        EventProposal proposal)
    {
        proposal.CourseId = model.CourseId;
        proposal.Title = model.Title.Trim();
        proposal.Description = model.Description.Trim();
        proposal.Reason = model.Reason.Trim();
        proposal.PreferredStartsAt = model.PreferredStartsAt?.ToUniversalTime();
        proposal.PreferredEndsAt = model.PreferredEndsAt?.ToUniversalTime();
        proposal.ProposedApplicationDeadline =
            model.ProposedApplicationDeadline?.ToUniversalTime();
        proposal.ProposedRegistrationAudience = model.ProposedRegistrationAudience;
        proposal.Mode = model.Mode;
        proposal.Location = model.Mode == EventMode.Online
            ? null
            : CleanOptionalText(model.Location);
        proposal.MeetingPlatform = model.Mode == EventMode.Physical
            ? null
            : model.MeetingPlatform;
        proposal.ProposedMaxParticipants = model.ProposedMaxParticipants;
        proposal.PublishAsAnnouncement = model.PublishAsAnnouncement;
    }

    private static string? CleanOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static AnnouncementAudience ToAnnouncementAudience(
        RegistrationAudience audience)
    {
        return audience switch
        {
            RegistrationAudience.Student => AnnouncementAudience.Student,
            RegistrationAudience.Tutor => AnnouncementAudience.Tutor,
            _ => AnnouncementAudience.All
        };
    }
}
