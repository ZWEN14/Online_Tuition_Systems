using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.Courses;

public sealed class TutorCourseListItemViewModel
{
    public int CourseId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public CourseStatus Status { get; init; }

    public string? RejectionReason { get; init; }

    public bool CanSubmit => Status is CourseStatus.Draft or CourseStatus.Rejected;

    public bool CanEdit => Status is CourseStatus.Draft or CourseStatus.Rejected;

    public bool CanArchive => Status == CourseStatus.Published;

    public DateTime UpdatedAtUtc { get; init; }
}
