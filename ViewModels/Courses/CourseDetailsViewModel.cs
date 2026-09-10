namespace Online_Tuition_Systems.ViewModels.Courses;

public sealed class CourseDetailsViewModel
{
    public int CourseId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public string TutorName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? ThumbnailPath { get; init; }

    public decimal Price { get; init; }

    public DateTime? PublishedAtUtc { get; init; }
}
