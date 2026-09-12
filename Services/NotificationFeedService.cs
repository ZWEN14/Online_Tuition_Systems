using AnywhereEdureach.Models;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.ViewModels.Notifications;

namespace Online_Tuition_Systems.Services;

// Combines the shared module notifications with the existing booking notification store.
// Each module keeps its current database schema and notification-writing logic.
public class NotificationFeedService(ApplicationDbContext context)
{
    private static readonly UserNotificationType[] ProposalNotificationTypes =
    [
        UserNotificationType.ProposalSubmitted,
        UserNotificationType.ProposalUpdated,
        UserNotificationType.ProposalChangesRequested,
        UserNotificationType.ProposalApproved,
        UserNotificationType.ProposalRejected
    ];

    public async Task<int> CountAsync(int userId, bool unreadOnly = false)
    {
        var moduleNotifications = VisibleModuleNotifications(userId);
        var bookingNotifications = context.BookingNotifications.Where(item => item.UserId == userId);

        if (unreadOnly)
        {
            moduleNotifications = moduleNotifications.Where(item => !item.ReadAt.HasValue);
            bookingNotifications = bookingNotifications.Where(item => !item.IsRead);
        }

        return await moduleNotifications.CountAsync() + await bookingNotifications.CountAsync();
    }

    public async Task<IReadOnlyList<UnifiedNotificationItem>> GetItemsAsync(
        int userId,
        bool unreadOnly,
        int skip,
        int take)
    {
        // The first skip + take entries from each source contain every possible
        // entry for that range after the two descending timelines are merged.
        var fetchCount = checked(skip + take);
        var moduleQuery = VisibleModuleNotifications(userId).AsNoTracking();
        var bookingQuery = context.BookingNotifications.AsNoTracking()
            .Where(item => item.UserId == userId);

        if (unreadOnly)
        {
            moduleQuery = moduleQuery.Where(item => !item.ReadAt.HasValue);
            bookingQuery = bookingQuery.Where(item => !item.IsRead);
        }

        var moduleItems = await moduleQuery
            .OrderByDescending(item => item.CreatedAt)
            .Take(fetchCount)
            .ToListAsync();
        var bookingItems = await bookingQuery
            .OrderByDescending(item => item.CreatedAt)
            .Take(fetchCount)
            .ToListAsync();

        return moduleItems.Select(ToFeedItem)
            .Concat(bookingItems.Select(ToFeedItem))
            .OrderByDescending(item => item.CreatedAt)
            .ThenBy(item => item.Source)
            .ThenByDescending(item => item.Id)
            .Skip(skip)
            .Take(take)
            .ToList();
    }

    public Task<UserNotification?> GetVisibleModuleNotificationAsync(int userId, int notificationId)
    {
        return VisibleModuleNotifications(userId)
            .SingleOrDefaultAsync(item => item.Id == notificationId);
    }

    private IQueryable<UserNotification> VisibleModuleNotifications(int userId)
    {
        return context.Notifications.Where(item =>
            item.UserId == userId
            && (!ProposalNotificationTypes.Contains(item.Type)
                || context.Users.Any(user => user.Id == userId && user.Role != UserRole.Student)));
    }

    private static UnifiedNotificationItem ToFeedItem(UserNotification item)
    {
        var category = item.Type switch
        {
            UserNotificationType.AnnouncementPublished or UserNotificationType.AnnouncementUpdated
                => "Announcement",
            UserNotificationType.EventPublished or UserNotificationType.EventUpdated
                or UserNotificationType.EventCancelled => "Event",
            UserNotificationType.ProposalSubmitted or UserNotificationType.ProposalUpdated
                or UserNotificationType.ProposalChangesRequested
                or UserNotificationType.ProposalApproved or UserNotificationType.ProposalRejected
                => "Proposal",
            UserNotificationType.ComplaintSubmitted or UserNotificationType.ComplaintAssigned
                or UserNotificationType.ComplaintStatusChanged => "Complaint",
            UserNotificationType.SurveyPublished or UserNotificationType.SurveyResponseSubmitted
                => "Survey",
            UserNotificationType.CourseSubmitted or UserNotificationType.CoursePublished
                or UserNotificationType.CourseRejected or UserNotificationType.CourseArchived
                or UserNotificationType.CourseSuspended or UserNotificationType.CourseRestored
                => "Course",
            UserNotificationType.EnrollmentCreated => "Enrollment",
            UserNotificationType.EnrollmentActivated => "Billing",
            _ => "Registration"
        };

        return new UnifiedNotificationItem
        {
            Id = item.Id,
            Category = category,
            Title = item.Title,
            Message = item.Message,
            Details = item.Details,
            CreatedAt = item.CreatedAt,
            IsUnread = item.IsUnread,
            IsUpdate = item.Type is UserNotificationType.AnnouncementUpdated
                or UserNotificationType.EventUpdated
                or UserNotificationType.ComplaintStatusChanged
                or UserNotificationType.CourseRejected
                or UserNotificationType.CourseArchived
                or UserNotificationType.CourseSuspended
                or UserNotificationType.CourseRestored
                or UserNotificationType.EnrollmentActivated
        };
    }

    private static UnifiedNotificationItem ToFeedItem(AnywhereEdureach.Models.Notification item)
    {
        // Booking timestamps are stored as local DateTime values by the mentor-mentee module.
        var localCreatedAt = item.CreatedAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(item.CreatedAt, DateTimeKind.Local)
            : item.CreatedAt;

        return new UnifiedNotificationItem
        {
            Id = item.Id,
            Source = "booking",
            Category = "Booking",
            Title = "Tuition booking",
            Message = item.Message,
            CreatedAt = new DateTimeOffset(localCreatedAt),
            IsUnread = !item.IsRead,
            IsUpdate = true
        };
    }
}
