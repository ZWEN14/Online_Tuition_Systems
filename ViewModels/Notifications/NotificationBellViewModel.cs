using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.Notifications;

public class NotificationBellViewModel
{
    public int UnreadCount { get; set; }

    public IReadOnlyList<UserNotification> RecentItems { get; set; }
        = Array.Empty<UserNotification>();
}
