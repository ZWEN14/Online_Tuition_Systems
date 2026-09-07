using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

public enum AnnouncementAudience
{
    All,
    Student,
    Tutor
}

public enum AnnouncementPriority
{
    Normal,
    Important,
    Urgent
}

public enum AnnouncementStatus
{
    Draft,
    Published,
    Archived
}

public class Announcement
{
    public int Id { get; set; }

    public int? CourseId { get; set; }
    public int? EventId { get; set; }

    public Event? Event { get; set; }

    [StringLength(450)]
    public string? CreatedByUserId { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(10_000)]
    public string Content { get; set; } = string.Empty;

    public AnnouncementAudience Audience { get; set; } = AnnouncementAudience.All;

    public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.Normal;

    public AnnouncementStatus Status { get; set; } = AnnouncementStatus.Draft;

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsPublished(DateTimeOffset currentTime)
    {
        return Status == AnnouncementStatus.Published
            && PublishedAt.HasValue
            && PublishedAt.Value <= currentTime
            && (!ExpiresAt.HasValue || ExpiresAt.Value > currentTime);
    }
}
