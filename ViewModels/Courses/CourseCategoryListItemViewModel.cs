namespace Online_Tuition_Systems.ViewModels.Courses;

public sealed class CourseCategoryListItemViewModel
{
    public int CourseCategoryId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsActive { get; init; }

    public int CourseCount { get; init; }
}
