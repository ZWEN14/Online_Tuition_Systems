using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AnnouncementPublishedAsync(Announcement announcement)
    {
        var userIds = await AudienceUserIds(announcement.Audience);

        AddNotifications(
            userIds,
            UserNotificationType.AnnouncementPublished,
            announcement.Title,
            "A new announcement has been published.",
            $"Priority: {announcement.Priority}",
            $"/Announcements/Details/{announcement.Id}");
    }

    public async Task AnnouncementUpdatedAsync(Announcement announcement)
    {
        var userIds = await AudienceUserIds(announcement.Audience);

        AddNotifications(
            userIds,
            UserNotificationType.AnnouncementUpdated,
            $"Announcement updated: {announcement.Title}",
            "A published announcement has been updated.",
            $"Priority: {announcement.Priority}",
            $"/Announcements/Details/{announcement.Id}");
    }

    public async Task EventPublishedAsync(Event tuitionEvent)
    {
        var hasPublishedAnnouncement = await _context.Announcements
            .AnyAsync(item =>
                item.EventId == tuitionEvent.Id
                && item.Status == AnnouncementStatus.Published);

        if (hasPublishedAnnouncement)
        {
            return;
        }

        var userIds = await AudienceUserIds(tuitionEvent.RegistrationAudience);

        AddNotifications(
            userIds,
            UserNotificationType.EventPublished,
            tuitionEvent.Title,
            "A new event has been published.",
            $"{tuitionEvent.Mode} event on {tuitionEvent.StartsAt.ToLocalTime():dd MMM yyyy, h:mm tt}",
            $"/Events/Details/{tuitionEvent.Id}");
    }

    public async Task EventUpdatedAsync(Event tuitionEvent)
    {
        var userIds = await AudienceUserIds(tuitionEvent.RegistrationAudience);
        var registrantUserIds = await _context.EventRegistrations
            .Where(item =>
                item.EventId == tuitionEvent.Id
                && (item.Status == EventRegistrationStatus.Pending
                    || item.Status == EventRegistrationStatus.Approved))
            .Select(item => item.UserId)
            .Distinct()
            .ToListAsync();

        userIds.AddRange(registrantUserIds);

        AddNotifications(
            userIds,
            UserNotificationType.EventUpdated,
            $"Event updated: {tuitionEvent.Title}",
            "A published event has been updated. Please review the latest details.",
            $"{tuitionEvent.Mode} event on {tuitionEvent.StartsAt.ToLocalTime():dd MMM yyyy, h:mm tt}",
            $"/Events/Details/{tuitionEvent.Id}");
    }

    public async Task EventCancelledAsync(Event tuitionEvent)
    {
        var registrantUserIds = await _context.EventRegistrations
            .Where(item =>
                item.EventId == tuitionEvent.Id
                && (item.Status == EventRegistrationStatus.Pending
                    || item.Status == EventRegistrationStatus.Approved))
            .Select(item => item.UserId)
            .Distinct()
            .ToListAsync();

        AddNotifications(
            registrantUserIds,
            UserNotificationType.EventCancelled,
            $"Event cancelled: {tuitionEvent.Title}",
            "An event you registered for has been cancelled.",
            $"Reason: {tuitionEvent.CancellationReason}",
            "/EventRegistrations/MyRegistrations");

        var organizerEmail = tuitionEvent.OrganizerUserId;

        if (string.IsNullOrWhiteSpace(organizerEmail))
        {
            return;
        }

        var organizerUserId = await UserIdForEmail(organizerEmail);

        if (organizerUserId.HasValue
            && !string.Equals(
                organizerEmail,
                tuitionEvent.CancelledByUserId,
                StringComparison.OrdinalIgnoreCase)
            && !registrantUserIds.Contains(organizerUserId.Value))
        {
            AddNotifications(
                new[] { organizerUserId.Value },
                UserNotificationType.EventCancelled,
                $"Event cancelled: {tuitionEvent.Title}",
                "An event you organise has been cancelled.",
                $"Reason: {tuitionEvent.CancellationReason}",
                $"/Events/Details/{tuitionEvent.Id}");
        }
    }

    public async Task ProposalSubmittedAsync(EventProposal proposal)
    {
        AddNotifications(
            await AdminUserIds(),
            UserNotificationType.ProposalSubmitted,
            $"New event proposal: {proposal.Title}",
            $"{proposal.ProposedByUserId} submitted an event proposal for review.",
            $"Audience: {proposal.RegistrationAudience}; mode: {proposal.Mode?.ToString() ?? "Incomplete"}",
            $"/EventProposals/Details/{proposal.Id}");
    }

    public async Task ProposalUpdatedAsync(EventProposal proposal)
    {
        AddNotifications(
            await AdminUserIds(),
            UserNotificationType.ProposalUpdated,
            $"Event proposal updated: {proposal.Title}",
            $"{proposal.ProposedByUserId} updated a pending proposal.",
            $"Revision: {proposal.RevisionCount}",
            $"/EventProposals/Details/{proposal.Id}");
    }

    public async Task ProposalReviewedAsync(EventProposal proposal)
    {
        var userId = await UserIdForEmail(proposal.ProposedByUserId);

        if (!userId.HasValue)
        {
            return;
        }

        var type = proposal.Status switch
        {
            EventProposalStatus.Approved => UserNotificationType.ProposalApproved,
            EventProposalStatus.ChangesRequested => UserNotificationType.ProposalChangesRequested,
            _ => UserNotificationType.ProposalRejected
        };

        AddNotifications(
            new[] { userId.Value },
            type,
            $"Event proposal {proposal.Status}: {proposal.Title}",
            proposal.Status == EventProposalStatus.ChangesRequested
                ? "Admin requested changes to your event proposal."
                : $"Your event proposal was {proposal.Status.ToString().ToLowerInvariant()}.",
            proposal.ReviewNote,
            $"/EventProposals/Details/{proposal.Id}");
    }

    public async Task RegistrationSubmittedAsync(EventRegistration registration)
    {
        var organizerUserIds = await OrganizerUserIds(registration.Event);

        AddNotifications(
            organizerUserIds,
            UserNotificationType.RegistrationSubmitted,
            $"New registration: {registration.Event.Title}",
            $"{registration.User.Email} submitted an event registration.",
            null,
            $"/EventRegistrations/Manage?eventId={registration.EventId}");
    }

    public async Task RegistrationUpdatedAsync(EventRegistration registration)
    {
        var organizerUserIds = await OrganizerUserIds(registration.Event);

        AddNotifications(
            organizerUserIds,
            UserNotificationType.RegistrationUpdated,
            $"Registration updated: {registration.Event.Title}",
            $"{registration.User.Email} updated an event registration.",
            null,
            $"/EventRegistrations/Manage?eventId={registration.EventId}");
    }

    public async Task RegistrationCancelledAsync(EventRegistration registration)
    {
        var organizerUserIds = await OrganizerUserIds(registration.Event);

        AddNotifications(
            organizerUserIds,
            UserNotificationType.RegistrationCancelled,
            $"Registration cancelled: {registration.Event.Title}",
            $"{registration.User.Email} cancelled an event registration.",
            null,
            $"/EventRegistrations/Manage?eventId={registration.EventId}");
    }

    public Task RegistrationReviewedAsync(EventRegistration registration)
    {
        var approved = registration.Status == EventRegistrationStatus.Approved;

        AddNotifications(
            new[] { registration.UserId },
            approved
                ? UserNotificationType.RegistrationApproved
                : UserNotificationType.RegistrationRejected,
            $"Registration {registration.Status}: {registration.Event.Title}",
            $"Your event registration was {registration.Status.ToString().ToLowerInvariant()}.",
            registration.ReviewNote,
            "/EventRegistrations/MyRegistrations");

        return Task.CompletedTask;
    }

    private Task<List<int>> AdminUserIds()
    {
        return _context.Users
            .AsNoTracking()
            .Where(item => item.Role == AppRoles.Admin)
            .Select(item => item.Id)
            .ToListAsync();
    }

    private async Task<List<int>> OrganizerUserIds(Event tuitionEvent)
    {
        if (string.IsNullOrWhiteSpace(tuitionEvent.OrganizerUserId))
        {
            return [];
        }

        var userId = await UserIdForEmail(tuitionEvent.OrganizerUserId);
        return userId.HasValue ? [userId.Value] : [];
    }

    private async Task<List<int>> AudienceUserIds(AnnouncementAudience audience)
    {
        var query = StudentAndTutorUsers();

        if (audience == AnnouncementAudience.Student)
        {
            query = query.Where(item => item.Role == AppRoles.Student);
        }
        else if (audience == AnnouncementAudience.Tutor)
        {
            query = query.Where(item => item.Role == AppRoles.Tutor);
        }

        return await query.Select(item => item.Id).ToListAsync();
    }

    private async Task<List<int>> AudienceUserIds(RegistrationAudience audience)
    {
        var query = StudentAndTutorUsers();

        if (audience == RegistrationAudience.Student)
        {
            query = query.Where(item => item.Role == AppRoles.Student);
        }
        else if (audience == RegistrationAudience.Tutor)
        {
            query = query.Where(item => item.Role == AppRoles.Tutor);
        }

        return await query.Select(item => item.Id).ToListAsync();
    }

    private IQueryable<UserAccount> StudentAndTutorUsers()
    {
        return _context.Users
            .AsNoTracking()
            .Where(item =>
                item.Role == AppRoles.Student
                || item.Role == AppRoles.Tutor);
    }

    private Task<int?> UserIdForEmail(string email)
    {
        return _context.Users
            .AsNoTracking()
            .Where(item => item.Email == email)
            .Select(item => (int?)item.Id)
            .SingleOrDefaultAsync();
    }

    private void AddNotifications(
        IEnumerable<int> userIds,
        UserNotificationType type,
        string title,
        string message,
        string? details,
        string? targetUrl)
    {
        var currentTime = DateTimeOffset.UtcNow;
        var notifications = userIds
            .Distinct()
            .Select(userId => new UserNotification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                Details = details,
                TargetUrl = targetUrl,
                CreatedAt = currentTime
            });

        _context.Notifications.AddRange(notifications);
    }
}
