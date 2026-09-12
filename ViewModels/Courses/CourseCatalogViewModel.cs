namespace Online_Tuition_Systems.ViewModels.Courses;

public sealed class CourseCatalogViewModel
{
    public string? Search { get; set; }

    public int? CategoryId { get; set; }

    public string Sort { get; set; } = "newest";

    public int Page { get; set; } = 1;

    public int TotalPages { get; init; }

    public int TotalCourses { get; init; }

    public IReadOnlyList<CourseCategoryOptionViewModel> Categories { get; init; } = [];

    public IReadOnlyList<CourseListItemViewModel> Courses { get; init; } = [];
}
