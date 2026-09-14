using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.ViewModels.Courses;

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

    public string ActiveTab { get; set; } = CourseWorkspaceTabs.Overview;

    public IReadOnlyList<CourseLessonItemViewModel> Lessons { get; init; } = [];

    public CourseStreamViewModel? Stream { get; set; }

    public CourseworkWorkspaceViewModel? Coursework { get; set; }
}
