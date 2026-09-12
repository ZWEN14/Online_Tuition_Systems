using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.Enrollments;

public sealed class StudentCoursesViewModel
{
    public IReadOnlyList<StudentCourseListItemViewModel> Courses { get; init; } = [];
}

public sealed class StudentCourseListItemViewModel
{
    public int EnrollmentId { get; init; }

    public int CourseId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string TutorName { get; init; } = string.Empty;

    public string? ThumbnailPath { get; init; }

    public decimal Price { get; init; }

    public EnrollmentStatus EnrollmentStatus { get; init; }

    public CourseStatus CourseStatus { get; init; }

    public DateTime EnrolledAtUtc { get; init; }

    public bool HasAccess { get; init; }
}
