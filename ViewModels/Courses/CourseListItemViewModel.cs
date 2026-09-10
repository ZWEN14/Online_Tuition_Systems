namespace Online_Tuition_Systems.ViewModels.Courses;

public sealed class CourseListItemViewModel
{
    public int CourseId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public string TutorName { get; init; } = string.Empty;

    public string? ShortDescription { get; init; }

    public string? ThumbnailPath { get; init; }

    public decimal Price { get; init; }
}
