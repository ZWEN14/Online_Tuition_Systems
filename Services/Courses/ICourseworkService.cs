using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Services.Courses;

public interface ICourseworkService
{
    Task<CourseworkWorkspaceViewModel?> GetForTutorAsync(int tutorId, int courseId, CancellationToken cancellationToken);
    Task<CourseworkWorkspaceViewModel?> GetForStudentAsync(int studentId, int courseId, CancellationToken cancellationToken);
    Task<CourseAssignmentFormViewModel?> GetAssignmentFormAsync(int tutorId, int courseId, int assignmentId, CancellationToken cancellationToken);
    Task<CourseActionResult> CreateAssignmentAsync(int tutorId, CourseAssignmentFormViewModel model, CancellationToken cancellationToken);
    Task<CourseActionResult> UpdateAssignmentAsync(int tutorId, CourseAssignmentFormViewModel model, CancellationToken cancellationToken);
    Task<CourseActionResult> PublishAssignmentAsync(int tutorId, int courseId, int assignmentId, CancellationToken cancellationToken);
    Task<CourseActionResult> DeleteAssignmentAsync(int tutorId, int courseId, int assignmentId, CancellationToken cancellationToken);
    Task<StudentAssignmentViewModel?> GetStudentAssignmentAsync(int studentId, int courseId, int assignmentId, CancellationToken cancellationToken);
    Task<CourseActionResult> SaveSubmissionAsync(int studentId, CourseSubmissionFormViewModel model, CancellationToken cancellationToken);
    Task<CourseworkSubmissionsViewModel?> GetSubmissionsAsync(int tutorId, int courseId, int assignmentId, CancellationToken cancellationToken);
    Task<CourseworkGradeViewModel?> GetGradeFormAsync(int tutorId, int courseId, int submissionId, CancellationToken cancellationToken);
    Task<CourseActionResult> GradeAsync(int tutorId, CourseworkGradeViewModel model, CancellationToken cancellationToken);
    Task<CourseworkFileDescriptor?> GetAssignmentFileAsync(int userId, bool isTutor, int assignmentId, CancellationToken cancellationToken);
    Task<CourseworkFileDescriptor?> GetSubmissionFileAsync(int userId, bool isTutor, int submissionId, CancellationToken cancellationToken);
}

public sealed record CourseworkFileDescriptor(
    string StoredName,
    string FileName,
    string ContentType);
