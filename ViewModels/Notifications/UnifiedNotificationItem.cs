namespace Online_Tuition_Systems.ViewModels.Notifications;

public class UnifiedNotificationItem
{
    public int Id { get; init; }

    public string Source { get; init; } = "module";

    public string Category { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? Details { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public bool IsUnread { get; init; }

    public bool IsUpdate { get; init; }
}
