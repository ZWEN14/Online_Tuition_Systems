using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.ViewModels.Notifications;

namespace Online_Tuition_Systems.Services;

// Combines the existing event/announcement and mentor-mentee notification stores.
// Neither module needs to change its database schema or notification-writing logic.
public class NotificationFeedService(ApplicationDbContext context)
{
    public async Task<int> CountAsync(int userId, bool unreadOnly = false)
    {
        var moduleNotifications = context.Notifications.Where(item => item.UserId == userId);
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
        var moduleQuery = context.Notifications.AsNoTracking()
            .Where(item => item.UserId == userId);
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
