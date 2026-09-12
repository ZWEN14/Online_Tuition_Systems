namespace Online_Tuition_Systems.ViewModels.Home;

public sealed class HomeDashboardViewModel
{
    public int PublishedCourseCount { get; set; }
    public int UpcomingEventCount { get; set; }
    public int UnreadNotificationCount { get; set; }
    public IReadOnlyList<HomeCoursePreview> Courses { get; set; } = [];
    public IReadOnlyList<HomeEventPreview> Events { get; set; } = [];
    public IReadOnlyList<HomeAnnouncementPreview> Announcements { get; set; } = [];
}

public sealed class HomeCoursePreview
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public sealed class HomeEventPreview
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
}

public sealed class HomeAnnouncementPreview
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; set; }
}
