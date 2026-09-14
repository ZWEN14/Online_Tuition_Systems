using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Services.Courses;

public interface ICourseWorkspaceService
{
    Task<TutorCourseWorkspaceViewModel?> GetTutorWorkspaceAsync(
        int tutorId,
        int courseId,
        CancellationToken cancellationToken);

    Task<CourseLessonFormViewModel?> GetLessonFormAsync(
        int tutorId,
        int courseId,
        int courseLessonId,
        CancellationToken cancellationToken);

    Task<CourseActionResult> CreateLessonAsync(
        int tutorId,
        CourseLessonFormViewModel model,
        CancellationToken cancellationToken);

    Task<CourseActionResult> UpdateLessonAsync(
        int tutorId,
        CourseLessonFormViewModel model,
        CancellationToken cancellationToken);

    Task<CourseActionResult> DeleteLessonAsync(
        int tutorId,
        int courseId,
        int courseLessonId,
        CancellationToken cancellationToken);
}
