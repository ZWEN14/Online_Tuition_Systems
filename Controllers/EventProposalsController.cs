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

[Authorize(Roles = AppRoles.AdminOrTutor)]
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
    [Authorize(Roles = AppRoles.Tutor)]
    public IActionResult Create()
    {
        return View(new EventProposalFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Tutor)]
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
    [Authorize(Roles = AppRoles.Tutor)]
    public async Task<IActionResult> Edit(int id)
    {
        var proposal = await FindOwnedEditableProposal(id, asNoTracking: true);

        if (proposal is null)
        {
            return NotFound();
        }

        return View(ToFormViewModel(proposal));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Tutor)]
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

        var proposal = await FindOwnedEditableProposal(id, asNoTracking: false);

        if (proposal is null)
        {
            return NotFound();
        }

        var wasChangesRequested = proposal.Status == EventProposalStatus.ChangesRequested;

        CopyFormToProposal(model, proposal);
        proposal.Status = EventProposalStatus.Pending;
        proposal.RevisionCount++;
        proposal.LastRevisedAt = DateTimeOffset.UtcNow;
        proposal.UpdatedAt = DateTimeOffset.UtcNow;

        if (wasChangesRequested)
        {
            proposal.ReviewedByUserId = null;
            proposal.ReviewedAt = null;
            proposal.ReviewNote = null;
        }

        await _notificationService.ProposalUpdatedAsync(proposal);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Proposal updated and resubmitted for Admin review.";
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

        var completenessError = GetProposalCompletenessError(proposal);

        if (completenessError is not null)
        {
            TempData["ErrorMessage"] = completenessError;
            return RedirectToAction(nameof(Details), new { id });
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

        completenessError = GetProposalCompletenessError(pendingProposal);

        if (completenessError is not null)
        {
            TempData["ErrorMessage"] = completenessError;
            return RedirectToAction(nameof(Details), new { id });
        }

        var currentTime = DateTimeOffset.UtcNow;
        var tuitionEvent = new Event
        {
            CourseId = pendingProposal.CourseId,
            CreatedByUserId = adminUserId,
            OrganizerUserId = pendingProposal.ProposedByUserId,
            Title = pendingProposal.Title,
            Description = pendingProposal.Description,
            StartsAt = pendingProposal.StartsAt!.Value,
            EndsAt = pendingProposal.EndsAt!.Value,
            ApplicationDeadline = pendingProposal.ApplicationDeadline,
            RegistrationAudience = pendingProposal.RegistrationAudience,
            Mode = pendingProposal.Mode!.Value,
            Location = pendingProposal.Mode == EventMode.Online
                ? null
                : CleanOptionalText(pendingProposal.Location),
            MeetingPlatform = pendingProposal.Mode == EventMode.Physical
                ? null
                : pendingProposal.MeetingPlatform,
            MeetingUrl = pendingProposal.Mode == EventMode.Physical
                ? null
                : CleanOptionalText(pendingProposal.MeetingUrl),
            MaxParticipants = pendingProposal.MaxParticipants!.Value,
            Status = EventStatus.Draft,
            CreatedAt = currentTime,
            UpdatedAt = currentTime
        };

        _context.Events.Add(tuitionEvent);

        pendingProposal.Status = EventProposalStatus.Approved;
        pendingProposal.ReviewedByUserId = adminUserId;
        pendingProposal.ReviewedAt = currentTime;
        pendingProposal.ReviewNote = CleanOptionalText(model.ReviewNote);
        pendingProposal.CreatedEvent = tuitionEvent;
        pendingProposal.UpdatedAt = currentTime;

        await _notificationService.ProposalReviewedAsync(pendingProposal);

        // One SaveChanges call creates the draft event, updates the proposal,
        // and inserts the notification in EF Core's implicit transaction.
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Proposal approved and a draft event was created.";
        return RedirectToAction("Details", "Events", new { id = tuitionEvent.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> RequestChanges(
        int id,
        [Bind(Prefix = "Changes")] RequestProposalChangesViewModel model)
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
            TempData["ErrorMessage"] = "Changes can only be requested for a pending proposal.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!ModelState.IsValid)
        {
            return View(
                "Details",
                BuildDetailsViewModel(proposal, changes: model));
        }

        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return Forbid();
        }

        var currentTime = DateTimeOffset.UtcNow;
        proposal.Status = EventProposalStatus.ChangesRequested;
        proposal.ReviewedByUserId = adminUserId;
        proposal.ReviewedAt = currentTime;
        proposal.ReviewNote = model.ReviewNote.Trim();
        proposal.UpdatedAt = currentTime;

        await _notificationService.ProposalReviewedAsync(proposal);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Changes were requested from the Tutor.";
        return RedirectToAction(nameof(Details), new { id });
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

        if (User.IsInRole(AppRoles.Tutor)
            && !string.IsNullOrWhiteSpace(userId))
        {
            return query.Where(item => item.ProposedByUserId == userId);
        }

        return query.Where(item => false);
    }

    private Task<EventProposal?> FindOwnedEditableProposal(int id, bool asNoTracking)
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
            && (item.Status == EventProposalStatus.Pending
                || item.Status == EventProposalStatus.ChangesRequested));
    }

    private static EventProposalDetailsViewModel BuildDetailsViewModel(
        EventProposal proposal,
        ApproveEventProposalViewModel? approval = null,
        RejectEventProposalViewModel? rejection = null,
        RequestProposalChangesViewModel? changes = null)
    {
        return new EventProposalDetailsViewModel
        {
            Proposal = proposal,
            Approval = approval ?? new ApproveEventProposalViewModel
            {
                Id = proposal.Id
            },
            Rejection = rejection ?? new RejectEventProposalViewModel
            {
                Id = proposal.Id
            },
            Changes = changes ?? new RequestProposalChangesViewModel
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
            StartsAt = proposal.StartsAt?.ToLocalTime(),
            EndsAt = proposal.EndsAt?.ToLocalTime(),
            ApplicationDeadline = proposal.ApplicationDeadline?.ToLocalTime(),
            RegistrationAudience = proposal.RegistrationAudience,
            Mode = proposal.Mode,
            Location = proposal.Location,
            MeetingPlatform = proposal.MeetingPlatform,
            MeetingUrl = proposal.MeetingUrl,
            MaxParticipants = proposal.MaxParticipants
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
        proposal.StartsAt = model.StartsAt?.ToUniversalTime();
        proposal.EndsAt = model.EndsAt?.ToUniversalTime();
        proposal.ApplicationDeadline = model.ApplicationDeadline?.ToUniversalTime();
        proposal.RegistrationAudience = model.RegistrationAudience;
        proposal.Mode = model.Mode;
        proposal.Location = model.Mode == EventMode.Online
            ? null
            : CleanOptionalText(model.Location);
        proposal.MeetingPlatform = model.Mode == EventMode.Physical
            ? null
            : model.MeetingPlatform;
        proposal.MeetingUrl = model.Mode == EventMode.Physical
            ? null
            : CleanOptionalText(model.MeetingUrl);
        proposal.MaxParticipants = model.MaxParticipants;
    }

    private static string? CleanOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? GetProposalCompletenessError(EventProposal proposal)
    {
        if (!proposal.StartsAt.HasValue
            || !proposal.EndsAt.HasValue
            || !proposal.ApplicationDeadline.HasValue
            || !proposal.Mode.HasValue
            || !proposal.MaxParticipants.HasValue)
        {
            return "The Tutor must complete the event schedule, deadline, mode and capacity before approval.";
        }

        if (proposal.StartsAt.Value <= DateTimeOffset.UtcNow
            || proposal.EndsAt.Value <= proposal.StartsAt.Value
            || proposal.ApplicationDeadline.Value <= DateTimeOffset.UtcNow
            || proposal.ApplicationDeadline.Value >= proposal.StartsAt.Value)
        {
            return "The proposed schedule or registration deadline is no longer valid. Request changes from the Tutor.";
        }

        if (proposal.MaxParticipants.Value < 1)
        {
            return "Maximum participants must be at least one.";
        }

        if (proposal.Mode is EventMode.Physical or EventMode.Hybrid
            && string.IsNullOrWhiteSpace(proposal.Location))
        {
            return "A physical or hybrid proposal requires a location.";
        }

        if (proposal.Mode is EventMode.Online or EventMode.Hybrid
            && !proposal.MeetingPlatform.HasValue)
        {
            return "An online or hybrid proposal requires a meeting platform.";
        }

        return null;
    }
}
