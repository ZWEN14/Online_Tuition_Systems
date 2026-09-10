using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Services.Courses;

public interface ICourseAdministrationService
{
    Task<IReadOnlyList<CourseCategoryListItemViewModel>> GetCategoriesAsync(
        CancellationToken cancellationToken);

    Task<CourseActionResult> CreateCategoryAsync(
        CourseCategoryFormViewModel model,
        CancellationToken cancellationToken);

    Task<CourseActionResult> ToggleCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminCourseReviewViewModel>> GetPendingReviewsAsync(
        CancellationToken cancellationToken);

    Task<AdminCourseReviewViewModel?> GetPendingReviewAsync(
        int courseId,
        CancellationToken cancellationToken);

    Task<CourseActionResult> ApproveAsync(
        int administratorId,
        int courseId,
        CancellationToken cancellationToken);

    Task<CourseActionResult> RejectAsync(
        int administratorId,
        int courseId,
        string reason,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminCourseManagementViewModel>> GetCoursesAsync(
        CancellationToken cancellationToken);

    Task<AdminCourseManagementViewModel?> GetCourseAsync(
        int courseId,
        CancellationToken cancellationToken);

    Task<CourseActionResult> SuspendAsync(
        int administratorId,
        int courseId,
        string reason,
        CancellationToken cancellationToken);

    Task<CourseActionResult> RestoreAsync(
        int administratorId,
        int courseId,
        CancellationToken cancellationToken);
}
