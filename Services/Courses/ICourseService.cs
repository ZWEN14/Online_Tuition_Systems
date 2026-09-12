using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Services.Courses;

public interface ICourseService
{
    Task<CourseCatalogViewModel> GetPublishedAsync(
        CourseCatalogViewModel query,
        CancellationToken cancellationToken);

    Task<CourseDetailsViewModel?> GetPublishedDetailsAsync(
        string slug,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TutorCourseListItemViewModel>> GetTutorCoursesAsync(
        int tutorId,
        CancellationToken cancellationToken);

    Task PopulateCategoriesAsync(
        CourseFormViewModel model,
        CancellationToken cancellationToken);

    Task<CourseCreateResult> CreateDraftAsync(
        int tutorId,
        CourseFormViewModel model,
        CancellationToken cancellationToken);

    Task<CourseActionResult> SubmitForReviewAsync(
        int tutorId,
        int courseId,
        CancellationToken cancellationToken);

    Task<CourseFormViewModel?> GetEditModelAsync(
        int tutorId,
        int courseId,
        CancellationToken cancellationToken);

    Task<CourseActionResult> UpdateDraftAsync(
        int tutorId,
        int courseId,
        CourseFormViewModel model,
        CancellationToken cancellationToken);

    Task<CourseActionResult> ArchiveAsync(
        int tutorId,
        int courseId,
        CancellationToken cancellationToken);
}

public sealed record CourseCreateResult(
    bool Succeeded,
    int? CourseId = null,
    string? Field = null,
    string? Error = null);

public sealed record CourseActionResult(
    bool Succeeded,
    string? Error = null);
