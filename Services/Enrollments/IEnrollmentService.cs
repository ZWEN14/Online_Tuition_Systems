using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.ViewModels.Enrollments;

namespace Online_Tuition_Systems.Services.Enrollments;

public interface IEnrollmentService
{
    Task<EnrollmentStatus?> GetStatusAsync(
        int studentId,
        int courseId,
        CancellationToken cancellationToken);

    Task<EnrollmentActionResult> EnrollAsync(
        int studentId,
        int courseId,
        CancellationToken cancellationToken);

    Task<StudentCoursesResult> GetStudentCoursesAsync(
        int studentId,
        CancellationToken cancellationToken);

    Task<CourseAccessResult> GetCourseAccessAsync(
        int studentId,
        int courseId,
        CancellationToken cancellationToken);
}

public sealed record EnrollmentActionResult(
    bool Succeeded,
    bool RequiresPayment = false,
    string? Message = null);

public sealed record StudentCoursesResult(
    bool Succeeded,
    StudentCoursesViewModel? Model = null);

public sealed record CourseAccessResult(
    bool Succeeded,
    CourseAccessViewModel? Model = null,
    string? Error = null);
