namespace Online_Tuition_Systems.ViewModels.Notifications;

public class NotificationBellViewModel
{
    public int UnreadCount { get; set; }

    public IReadOnlyList<UnifiedNotificationItem> RecentItems { get; set; }
        = Array.Empty<UnifiedNotificationItem>();
}
