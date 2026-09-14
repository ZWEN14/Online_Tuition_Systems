using System.ComponentModel.DataAnnotations;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.Courses;

public static class CourseStreamSortOptions
{
    public const string Latest = "latest";
    public const string Oldest = "oldest";

    public static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() == Oldest
            ? Oldest
            : Latest;
    }
}

public sealed class CourseStreamViewModel
{
    public int CourseId { get; init; }
    public bool IsTutor { get; init; }
    public int CurrentUserId { get; init; }
    public bool CanContribute { get; init; }
    public string Sort { get; init; } = CourseStreamSortOptions.Latest;
    public IReadOnlyList<CourseStreamEventViewModel> UpcomingEvents { get; init; } = [];
    public IReadOnlyList<CourseStreamPostViewModel> Posts { get; init; } = [];
}

public sealed class CourseStreamEventViewModel
{
    public int EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public EventMode Mode { get; init; }
    public DateTimeOffset StartsAt { get; init; }
    public DateTimeOffset EndsAt { get; init; }
}

public sealed class CourseStreamPostViewModel
{
    public int AnnouncementId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string TutorName { get; init; } = string.Empty;
    public string? TutorPhotoPath { get; init; }
    public DateTimeOffset PublishedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public bool WasEdited => UpdatedAt > PublishedAt;
    public IReadOnlyList<CourseStreamCommentViewModel> Comments { get; init; } = [];
}

public sealed class CourseStreamCommentViewModel
{
    public int CourseStreamCommentId { get; init; }
    public int UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string? UserPhotoPath { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public bool CanEdit { get; init; }
    public bool IsRemovedByTutor { get; init; }
    public DateTime? RemovedAtUtc { get; init; }
    public string? RemovedByTutorName { get; init; }
    public bool WasEdited => UpdatedAtUtc > CreatedAtUtc;
}

public sealed class CourseStreamPostFormViewModel
{
    public int CourseId { get; set; }
    public int? AnnouncementId { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(10000)]
    [DataType(DataType.MultilineText)]
    public string Content { get; set; } = string.Empty;
}

public sealed class CourseStreamCommentFormViewModel
{
    public int CourseId { get; set; }
    public int AnnouncementId { get; set; }

    [Required, StringLength(2000)]
    [Display(Name = "Comment")]
    public string Content { get; set; } = string.Empty;
}

public sealed class CourseStreamCommentEditViewModel
{
    public int CourseId { get; set; }
    public int CourseStreamCommentId { get; set; }

    [Required, StringLength(2000)]
    [Display(Name = "Comment")]
    public string Content { get; set; } = string.Empty;
}
