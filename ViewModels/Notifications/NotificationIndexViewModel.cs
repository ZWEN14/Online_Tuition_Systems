using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.Notifications;

public class NotificationIndexViewModel
{
    public string Filter { get; set; } = "all";

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public int TotalCount { get; set; }

    public int UnreadCount { get; set; }

    public IReadOnlyList<UserNotification> Items { get; set; }
        = Array.Empty<UserNotification>();

    public int TotalPages => Math.Max(
        1,
        (int)Math.Ceiling(TotalCount / (double)PageSize));
}
