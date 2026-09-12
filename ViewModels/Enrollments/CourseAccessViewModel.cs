using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.Enrollments;

public sealed class CourseAccessViewModel
{
    public int CourseId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string TutorName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? ThumbnailPath { get; init; }

    public CourseStatus CourseStatus { get; init; }
}
