using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Services.Courses;

public interface ICourseStreamService
{
    Task<CourseStreamViewModel?> GetForTutorAsync(int tutorId, int courseId, string? sort, CancellationToken cancellationToken);
    Task<CourseStreamViewModel?> GetForStudentAsync(int studentId, int courseId, string? sort, CancellationToken cancellationToken);
    Task<CourseActionResult> CreatePostAsync(int tutorId, CourseStreamPostFormViewModel model, CancellationToken cancellationToken);
    Task<CourseStreamPostFormViewModel?> GetPostFormAsync(int tutorId, int courseId, int announcementId, CancellationToken cancellationToken);
    Task<CourseActionResult> UpdatePostAsync(int tutorId, CourseStreamPostFormViewModel model, CancellationToken cancellationToken);
    Task<CourseActionResult> DeletePostAsync(int tutorId, int courseId, int announcementId, CancellationToken cancellationToken);
    Task<CourseActionResult> AddCommentAsync(int userId, bool isTutor, CourseStreamCommentFormViewModel model, CancellationToken cancellationToken);
    Task<CourseStreamCommentEditViewModel?> GetCommentFormAsync(int userId, bool isTutor, int courseId, int commentId, CancellationToken cancellationToken);
    Task<CourseActionResult> UpdateCommentAsync(int userId, bool isTutor, CourseStreamCommentEditViewModel model, CancellationToken cancellationToken);
    Task<CourseActionResult> DeleteCommentAsync(int tutorId, int courseId, int commentId, CancellationToken cancellationToken);
}
