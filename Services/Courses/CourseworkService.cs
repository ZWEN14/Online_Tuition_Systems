using AnywhereEdureach.Models;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Services.Courses;

public sealed class CourseworkService(
    ApplicationDbContext dbContext,
    ICourseworkFileStorage fileStorage) : ICourseworkService
{
    public async Task<CourseworkWorkspaceViewModel?> GetForTutorAsync(
        int tutorId,
        int courseId,
        CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.AsNoTracking()
            .Where(item => item.CourseId == courseId
                && item.TutorId == tutorId
                && item.Tutor.Role == UserRole.Tutor
                && !item.Tutor.IsBlocked)
            .Select(item => new { item.Status })
            .SingleOrDefaultAsync(cancellationToken);
        if (course is null)
        {
            return null;
        }

        var assignments = await dbContext.CourseAssignments.AsNoTracking()
            .Where(item => item.CourseId == courseId)
            .OrderBy(item => item.DueAtUtc)
            .ThenBy(item => item.CourseAssignmentId)
            .Select(item => new CourseAssignmentItemViewModel
            {
                CourseAssignmentId = item.CourseAssignmentId,
                Title = item.Title,
                Instructions = item.Instructions,
                DueAtUtc = item.DueAtUtc,
                IsGraded = item.IsGraded,
                MaxMarks = item.MaxMarks,
                IsPublished = item.IsPublished,
                AttachmentFileName = item.AttachmentFileName,
                SubmissionCount = item.Submissions.Count,
                GradedCount = item.Submissions.Count(submission =>
                    submission.Score.HasValue || submission.GradedAtUtc.HasValue)
            })
            .ToListAsync(cancellationToken);

        return new CourseworkWorkspaceViewModel
        {
            CourseId = courseId,
            IsTutor = true,
            CanManage = course.Status != CourseStatus.Suspended,
            CanSubmit = false,
            Assignments = assignments
        };
    }

    public async Task<CourseworkWorkspaceViewModel?> GetForStudentAsync(
        int studentId,
        int courseId,
        CancellationToken cancellationToken)
    {
        var access = await dbContext.Enrollments.AsNoTracking()
            .Where(item => item.StudentId == studentId
                && item.CourseId == courseId
                && item.Status == EnrollmentStatus.Active
                && item.Student.Role == UserRole.Student
                && !item.Student.IsBlocked
                && (item.Course.Status == CourseStatus.Published
                    || item.Course.Status == CourseStatus.Archived))
            .Select(item => new { item.Course.Status })
            .SingleOrDefaultAsync(cancellationToken);
        if (access is null)
        {
            return null;
        }

        var assignments = await dbContext.CourseAssignments.AsNoTracking()
            .Where(item => item.CourseId == courseId && item.IsPublished)
            .OrderBy(item => item.DueAtUtc)
            .ThenBy(item => item.CourseAssignmentId)
            .Select(item => new CourseAssignmentItemViewModel
            {
                CourseAssignmentId = item.CourseAssignmentId,
                Title = item.Title,
                Instructions = item.Instructions,
                DueAtUtc = item.DueAtUtc,
                IsGraded = item.IsGraded,
                MaxMarks = item.MaxMarks,
                IsPublished = item.IsPublished,
                AttachmentFileName = item.AttachmentFileName,
                MySubmission = item.Submissions
                    .Where(submission => submission.StudentId == studentId)
                    .Select(submission => new CourseSubmissionSummaryViewModel
                    {
                        CourseSubmissionId = submission.CourseSubmissionId,
                        SubmittedAtUtc = submission.SubmittedAtUtc,
                        UpdatedAtUtc = submission.UpdatedAtUtc,
                        Score = submission.Score,
                        TutorFeedback = submission.TutorFeedback,
                        AttachmentFileName = submission.AttachmentFileName,
                        IsLate = submission.IsLate
                    })
                    .SingleOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new CourseworkWorkspaceViewModel
        {
            CourseId = courseId,
            IsTutor = false,
            CanManage = false,
            CanSubmit = access.Status == CourseStatus.Published,
            Assignments = assignments
        };
    }

    public Task<CourseAssignmentFormViewModel?> GetAssignmentFormAsync(
        int tutorId,
        int courseId,
        int assignmentId,
        CancellationToken cancellationToken)
    {
        return dbContext.CourseAssignments.AsNoTracking()
            .Where(item => item.CourseAssignmentId == assignmentId
                && item.CourseId == courseId
                && item.Course.TutorId == tutorId
                && item.Course.Status != CourseStatus.Suspended)
            .Select(item => new CourseAssignmentFormViewModel
            {
                CourseId = item.CourseId,
                CourseAssignmentId = item.CourseAssignmentId,
                Title = item.Title,
                Instructions = item.Instructions,
                DueAtMyt = ToMyt(item.DueAtUtc),
                IsGraded = item.IsGraded,
                MaxMarks = item.MaxMarks,
                PublishingMode = item.IsPublished
                    ? AssignmentPublishingMode.Publish
                    : AssignmentPublishingMode.Draft,
                IsPublished = item.IsPublished,
                ExistingAttachmentFileName = item.AttachmentFileName
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CourseActionResult> CreateAssignmentAsync(
        int tutorId,
        CourseAssignmentFormViewModel model,
        CancellationToken cancellationToken)
    {
        var course = await GetEditableCourseAsync(tutorId, model.CourseId, cancellationToken);
        if (course is null)
        {
            return new CourseActionResult(false, "Course not found or Coursework changes are unavailable.");
        }

        var publishNow = model.PublishingMode == AssignmentPublishingMode.Publish;
        if (publishNow && course.Status != CourseStatus.Published)
        {
            return new CourseActionResult(
                false,
                "The Course must be published before this assignment can be published.",
                nameof(model.PublishingMode));
        }
        if (model.IsGraded && !model.MaxMarks.HasValue)
        {
            return new CourseActionResult(false, "Enter the marks for this graded assignment.", nameof(model.MaxMarks));
        }

        var fileResult = await TrySaveFileAsync(model.Attachment, cancellationToken);
        if (fileResult.Error is not null)
        {
            return new CourseActionResult(false, fileResult.Error, nameof(model.Attachment));
        }

        var now = DateTime.UtcNow;
        dbContext.CourseAssignments.Add(new CourseAssignment
        {
            CourseId = model.CourseId,
            Title = model.Title.Trim(),
            Instructions = model.Instructions.Trim(),
            DueAtUtc = ToUtc(model.DueAtMyt),
            IsGraded = model.IsGraded,
            MaxMarks = model.IsGraded ? model.MaxMarks : null,
            IsPublished = publishNow,
            AttachmentFileName = fileResult.File?.OriginalName,
            AttachmentStoredName = fileResult.File?.StoredName,
            AttachmentContentType = fileResult.File?.ContentType,
            AttachmentSizeBytes = fileResult.File?.SizeBytes,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        course.UpdatedAtUtc = now;
        var result = await SaveAsync("The assignment could not be created.", cancellationToken);
        if (!result.Succeeded)
        {
            await fileStorage.DeleteAsync(fileResult.File?.StoredName);
        }
        return result;
    }

    public async Task<CourseActionResult> UpdateAssignmentAsync(
        int tutorId,
        CourseAssignmentFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!model.CourseAssignmentId.HasValue)
        {
            return new CourseActionResult(false, "Assignment not found.");
        }

        var assignment = await dbContext.CourseAssignments
            .Include(item => item.Course)
            .Include(item => item.Submissions)
            .SingleOrDefaultAsync(item => item.CourseAssignmentId == model.CourseAssignmentId
                && item.CourseId == model.CourseId
                && item.Course.TutorId == tutorId
                && item.Course.Status != CourseStatus.Suspended,
                cancellationToken);
        if (assignment is null)
        {
            return new CourseActionResult(false, "Assignment not found or Coursework changes are unavailable.");
        }


        var publishNow = model.PublishingMode == AssignmentPublishingMode.Publish;
        if (publishNow && assignment.Course.Status != CourseStatus.Published)
        {
            return new CourseActionResult(
                false,
                "The Course must be published before this assignment can be published.",
                nameof(model.PublishingMode));
        }
        if (!publishNow && assignment.IsPublished
            && await dbContext.CourseSubmissions.AnyAsync(item =>
                item.CourseAssignmentId == assignment.CourseAssignmentId,
                cancellationToken))
        {
            return new CourseActionResult(
                false,
                "A published assignment with submissions cannot be returned to draft.",
                nameof(model.PublishingMode));
        }
        if (model.IsGraded && !model.MaxMarks.HasValue)
        {
            return new CourseActionResult(false, "Enter the marks for this graded assignment.", nameof(model.MaxMarks));
        }
        if (model.IsGraded && assignment.Submissions.Any(item =>
            item.Score.HasValue && item.Score.Value > model.MaxMarks!.Value))
        {
            return new CourseActionResult(
                false,
                "The marks cannot be lower than a result already given to a Student.",
                nameof(model.MaxMarks));
        }

        var fileResult = await TrySaveFileAsync(model.Attachment, cancellationToken);
        if (fileResult.Error is not null)
        {
            return new CourseActionResult(false, fileResult.Error, nameof(model.Attachment));
        }

        var previousStoredName = assignment.AttachmentStoredName;
        var replaceOrRemove = fileResult.File is not null || model.RemoveAttachment;
        assignment.Title = model.Title.Trim();
        assignment.Instructions = model.Instructions.Trim();
        assignment.DueAtUtc = ToUtc(model.DueAtMyt);
        assignment.IsGraded = model.IsGraded;
        assignment.MaxMarks = model.IsGraded ? model.MaxMarks : null;
        assignment.IsPublished = publishNow;
        if (!model.IsGraded)
        {
            foreach (var submission in assignment.Submissions)
            {
                submission.Score = null;
                submission.GradedAtUtc = string.IsNullOrWhiteSpace(submission.TutorFeedback)
                    ? null
                    : submission.GradedAtUtc;
            }
        }
        if (replaceOrRemove)
        {
            assignment.AttachmentFileName = fileResult.File?.OriginalName;
            assignment.AttachmentStoredName = fileResult.File?.StoredName;
            assignment.AttachmentContentType = fileResult.File?.ContentType;
            assignment.AttachmentSizeBytes = fileResult.File?.SizeBytes;
        }
        assignment.UpdatedAtUtc = DateTime.UtcNow;
        assignment.Course.UpdatedAtUtc = assignment.UpdatedAtUtc;

        var result = await SaveAsync("The assignment could not be updated.", cancellationToken);
        if (!result.Succeeded)
        {
            await fileStorage.DeleteAsync(fileResult.File?.StoredName);
            return result;
        }
        if (replaceOrRemove)
        {
            await fileStorage.DeleteAsync(previousStoredName);
        }
        return result;
    }

    public async Task<CourseActionResult> PublishAssignmentAsync(
        int tutorId,
        int courseId,
        int assignmentId,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.CourseAssignments
            .SingleOrDefaultAsync(item => item.CourseAssignmentId == assignmentId
                && item.CourseId == courseId
                && item.Course.TutorId == tutorId
                && item.Course.Status == CourseStatus.Published,
                cancellationToken);
        if (assignment is null || assignment.DueAtUtc <= DateTime.UtcNow)
        {
            return new CourseActionResult(false, "Only a future assignment in your published Course can be published.");
        }

        assignment.IsPublished = true;
        assignment.UpdatedAtUtc = DateTime.UtcNow;
        return await SaveAsync("The assignment could not be published.", cancellationToken);
    }

    public async Task<CourseActionResult> DeleteAssignmentAsync(
        int tutorId,
        int courseId,
        int assignmentId,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.CourseAssignments
            .SingleOrDefaultAsync(item => item.CourseAssignmentId == assignmentId
                && item.CourseId == courseId
                && item.Course.TutorId == tutorId
                && item.Course.Status != CourseStatus.Suspended,
                cancellationToken);
        if (assignment is null || assignment.IsPublished
            || await dbContext.CourseSubmissions.AnyAsync(item =>
                item.CourseAssignmentId == assignmentId, cancellationToken))
        {
            return new CourseActionResult(false, "Only a draft assignment without submissions can be removed.");
        }

        var storedName = assignment.AttachmentStoredName;
        dbContext.CourseAssignments.Remove(assignment);
        var result = await SaveAsync("The assignment could not be removed.", cancellationToken);
        if (result.Succeeded)
        {
            await fileStorage.DeleteAsync(storedName);
        }
        return result;
    }

    public async Task<StudentAssignmentViewModel?> GetStudentAssignmentAsync(
        int studentId,
        int courseId,
        int assignmentId,
        CancellationToken cancellationToken)
    {
        var hasAccess = await HasStudentAccessAsync(studentId, courseId, cancellationToken);
        if (!hasAccess)
        {
            return null;
        }

        var assignment = await dbContext.CourseAssignments.AsNoTracking()
            .Where(item => item.CourseAssignmentId == assignmentId
                && item.CourseId == courseId
                && item.IsPublished)
            .Select(item => new
            {
                item.CourseAssignmentId,
                item.Instructions,
                item.DueAtUtc,
                item.IsGraded,
                item.MaxMarks,
                item.AttachmentFileName,
                CanSubmit = item.Course.Status == CourseStatus.Published,
                CourseTitle = item.Course.Title,
                AssignmentTitle = item.Title
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (assignment is null)
        {
            return null;
        }

        var submission = await dbContext.CourseSubmissions.AsNoTracking()
            .Where(item => item.CourseAssignmentId == assignmentId
                && item.StudentId == studentId)
            .Select(item => new CourseSubmissionFormViewModel
            {
                CourseSubmissionId = item.CourseSubmissionId,
                CourseId = courseId,
                CourseAssignmentId = assignmentId,
                TextResponse = item.TextResponse,
                ExistingAttachmentFileName = item.AttachmentFileName,
                SubmittedAtUtc = item.SubmittedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc,
                IsLate = item.IsLate,
                Score = item.Score,
                TutorFeedback = item.TutorFeedback
            })
            .SingleOrDefaultAsync(cancellationToken) ?? new CourseSubmissionFormViewModel
            {
                CourseId = courseId,
                CourseAssignmentId = assignmentId
            };

        return new StudentAssignmentViewModel
        {
            CourseId = courseId,
            CourseAssignmentId = assignment.CourseAssignmentId,
            CourseTitle = assignment.CourseTitle,
            Title = assignment.AssignmentTitle,
            Instructions = assignment.Instructions,
            DueAtUtc = assignment.DueAtUtc,
            IsGraded = assignment.IsGraded,
            MaxMarks = assignment.MaxMarks,
            AssignmentAttachmentFileName = assignment.AttachmentFileName,
            CanSubmit = assignment.CanSubmit,
            Submission = submission
        };
    }

    public async Task<CourseActionResult> SaveSubmissionAsync(
        int studentId,
        CourseSubmissionFormViewModel model,
        CancellationToken cancellationToken)
    {
        var assignment = await dbContext.CourseAssignments
            .Include(item => item.Course)
            .SingleOrDefaultAsync(item => item.CourseAssignmentId == model.CourseAssignmentId
                && item.CourseId == model.CourseId
                && item.IsPublished
                && item.Course.Status == CourseStatus.Published,
                cancellationToken);
        var activeStudent = assignment is not null
            && await dbContext.Enrollments.AnyAsync(item => item.StudentId == studentId
                && item.CourseId == model.CourseId
                && item.Status == EnrollmentStatus.Active
                && item.Student.Role == UserRole.Student
                && !item.Student.IsBlocked,
                cancellationToken);
        if (assignment is null || !activeStudent)
        {
            return new CourseActionResult(false, "The assignment is unavailable.");
        }
        var submission = await dbContext.CourseSubmissions.SingleOrDefaultAsync(item =>
            item.CourseAssignmentId == model.CourseAssignmentId
            && item.StudentId == studentId,
            cancellationToken);
        var fileResult = await TrySaveFileAsync(model.Attachment, cancellationToken);
        if (fileResult.Error is not null)
        {
            return new CourseActionResult(false, fileResult.Error, nameof(model.Attachment));
        }

        var keepsFile = fileResult.File is not null
            || (!model.RemoveAttachment
                && !string.IsNullOrWhiteSpace(submission?.AttachmentStoredName));
        if (string.IsNullOrWhiteSpace(model.TextResponse) && !keepsFile)
        {
            await fileStorage.DeleteAsync(fileResult.File?.StoredName);
            return new CourseActionResult(false, "Add a written response or attach a file.");
        }

        var now = DateTime.UtcNow;
        var isNewSubmission = submission is null;
        if (submission is null)
        {
            submission = new CourseSubmission
            {
                CourseAssignmentId = model.CourseAssignmentId,
                StudentId = studentId,
                SubmittedAtUtc = now
            };
            dbContext.CourseSubmissions.Add(submission);
        }

        var previousStoredName = submission.AttachmentStoredName;
        submission.TextResponse = NullIfWhiteSpace(model.TextResponse);
        var replaceOrRemoveFile = fileResult.File is not null || model.RemoveAttachment;
        if (replaceOrRemoveFile)
        {
            submission.AttachmentFileName = fileResult.File?.OriginalName;
            submission.AttachmentStoredName = fileResult.File?.StoredName;
            submission.AttachmentContentType = fileResult.File?.ContentType;
            submission.AttachmentSizeBytes = fileResult.File?.SizeBytes;
        }
        submission.UpdatedAtUtc = now;
        submission.IsLate = now >= assignment.DueAtUtc;
        submission.Score = null;
        submission.TutorFeedback = null;
        submission.GradedAtUtc = null;

        dbContext.Notifications.Add(new UserNotification
        {
            UserId = assignment.Course.TutorId,
            Type = UserNotificationType.CourseworkSubmitted,
            Title = $"Coursework {(isNewSubmission ? "submitted" : "updated")}: {assignment.Title}",
            Message = submission.IsLate
                ? "A Student submitted Coursework after the deadline."
                : "A Student submitted Coursework.",
            Details = submission.IsLate ? "Late submission" : "Submitted on time",
            TargetUrl = $"/Coursework/Submissions?courseId={assignment.CourseId}&assignmentId={assignment.CourseAssignmentId}",
            CreatedAt = DateTimeOffset.UtcNow
        });

        var result = await SaveAsync("The submission could not be saved.", cancellationToken);
        if (!result.Succeeded)
        {
            await fileStorage.DeleteAsync(fileResult.File?.StoredName);
            return result;
        }
        if (replaceOrRemoveFile)
        {
            await fileStorage.DeleteAsync(previousStoredName);
        }
        return result;
    }

    public async Task<CourseworkSubmissionsViewModel?> GetSubmissionsAsync(
        int tutorId,
        int courseId,
        int assignmentId,
        CancellationToken cancellationToken)
    {
        return await dbContext.CourseAssignments.AsNoTracking()
            .Where(item => item.CourseAssignmentId == assignmentId
                && item.CourseId == courseId
                && item.Course.TutorId == tutorId
                && item.Course.Tutor.Role == UserRole.Tutor
                && !item.Course.Tutor.IsBlocked)
            .Select(item => new CourseworkSubmissionsViewModel
            {
                CourseId = courseId,
                CourseAssignmentId = assignmentId,
                CourseTitle = item.Course.Title,
                AssignmentTitle = item.Title,
                IsGraded = item.IsGraded,
                MaxMarks = item.MaxMarks,
                ActiveEnrollmentCount = item.Course.Enrollments.Count(enrollment =>
                    enrollment.Status == EnrollmentStatus.Active),
                Submissions = item.Submissions
                    .OrderBy(submission => submission.Student.Name)
                    .Select(submission => new CourseworkSubmissionItemViewModel
                    {
                        CourseSubmissionId = submission.CourseSubmissionId,
                        StudentName = submission.Student.Name,
                        StudentEmail = submission.Student.Email,
                        SubmittedAtUtc = submission.SubmittedAtUtc,
                        UpdatedAtUtc = submission.UpdatedAtUtc,
                        IsLate = submission.IsLate,
                        TextResponse = submission.TextResponse,
                        AttachmentFileName = submission.AttachmentFileName,
                        Score = submission.Score,
                        TutorFeedback = submission.TutorFeedback
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<CourseworkGradeViewModel?> GetGradeFormAsync(
        int tutorId,
        int courseId,
        int submissionId,
        CancellationToken cancellationToken)
    {
        return dbContext.CourseSubmissions.AsNoTracking()
            .Where(item => item.CourseSubmissionId == submissionId
                && item.Assignment.CourseId == courseId
                && item.Assignment.Course.TutorId == tutorId
                && item.Assignment.Course.Status == CourseStatus.Published)
            .Select(item => new CourseworkGradeViewModel
            {
                CourseId = courseId,
                CourseAssignmentId = item.CourseAssignmentId,
                CourseSubmissionId = item.CourseSubmissionId,
                AssignmentTitle = item.Assignment.Title,
                StudentName = item.Student.Name,
                IsGraded = item.Assignment.IsGraded,
                MaxMarks = item.Assignment.MaxMarks,
                Score = item.Score,
                TutorFeedback = item.TutorFeedback
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CourseActionResult> GradeAsync(
        int tutorId,
        CourseworkGradeViewModel model,
        CancellationToken cancellationToken)
    {
        var submission = await dbContext.CourseSubmissions
            .Include(item => item.Assignment)
            .ThenInclude(item => item.Course)
            .SingleOrDefaultAsync(item => item.CourseSubmissionId == model.CourseSubmissionId
                && item.Assignment.CourseId == model.CourseId
                && item.Assignment.Course.TutorId == tutorId
                && item.Assignment.Course.Status == CourseStatus.Published,
                cancellationToken);
        if (submission is null)
        {
            return new CourseActionResult(false, "Submission not found or marking is unavailable.");
        }
        if (submission.Assignment.IsGraded
            && (!model.Score.HasValue
                || !submission.Assignment.MaxMarks.HasValue
                || model.Score.Value > submission.Assignment.MaxMarks.Value))
        {
            return new CourseActionResult(false,
                $"Enter a mark from 0 to {submission.Assignment.MaxMarks:0.##}.",
                nameof(model.Score));
        }

        submission.Score = submission.Assignment.IsGraded ? model.Score : null;
        submission.TutorFeedback = NullIfWhiteSpace(model.TutorFeedback);
        submission.GradedAtUtc = DateTime.UtcNow;
        return await SaveAsync("The result could not be saved.", cancellationToken);
    }

    public async Task<CourseworkFileDescriptor?> GetAssignmentFileAsync(
        int userId,
        bool isTutor,
        int assignmentId,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.CourseAssignments.AsNoTracking()
            .Where(assignment => assignment.CourseAssignmentId == assignmentId
                && assignment.AttachmentStoredName != null
                && assignment.AttachmentFileName != null
                && assignment.AttachmentContentType != null)
            .Select(assignment => new
            {
                Assignment = assignment,
                assignment.CourseId,
                assignment.Course.TutorId,
                CourseStatus = assignment.Course.Status
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            return null;
        }

        var allowed = isTutor
            ? item.TutorId == userId && item.CourseStatus != CourseStatus.Suspended
            : item.Assignment.IsPublished
                && item.CourseStatus is CourseStatus.Published or CourseStatus.Archived
                && await HasStudentAccessAsync(userId, item.CourseId, cancellationToken);
        return allowed
            ? new CourseworkFileDescriptor(
                item.Assignment.AttachmentStoredName!,
                item.Assignment.AttachmentFileName!,
                item.Assignment.AttachmentContentType!)
            : null;
    }

    public async Task<CourseworkFileDescriptor?> GetSubmissionFileAsync(
        int userId,
        bool isTutor,
        int submissionId,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.CourseSubmissions.AsNoTracking()
            .Where(submission => submission.CourseSubmissionId == submissionId
                && submission.AttachmentStoredName != null
                && submission.AttachmentFileName != null
                && submission.AttachmentContentType != null)
            .Select(submission => new
            {
                Submission = submission,
                submission.StudentId,
                submission.Assignment.CourseId,
                TutorId = submission.Assignment.Course.TutorId,
                CourseStatus = submission.Assignment.Course.Status
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            return null;
        }

        var allowed = isTutor
            ? item.TutorId == userId && item.CourseStatus != CourseStatus.Suspended
            : item.StudentId == userId
                && item.CourseStatus is CourseStatus.Published or CourseStatus.Archived
                && await HasStudentAccessAsync(userId, item.CourseId, cancellationToken);
        return allowed
            ? new CourseworkFileDescriptor(
                item.Submission.AttachmentStoredName!,
                item.Submission.AttachmentFileName!,
                item.Submission.AttachmentContentType!)
            : null;
    }

    private Task<Course?> GetEditableCourseAsync(
        int tutorId,
        int courseId,
        CancellationToken cancellationToken)
    {
        return dbContext.Courses.SingleOrDefaultAsync(item => item.CourseId == courseId
            && item.TutorId == tutorId
            && item.Status != CourseStatus.Suspended
            && item.Tutor.Role == UserRole.Tutor
            && !item.Tutor.IsBlocked,
            cancellationToken);
    }

    private Task<bool> HasStudentAccessAsync(
        int studentId,
        int courseId,
        CancellationToken cancellationToken)
    {
        return dbContext.Enrollments.AsNoTracking().AnyAsync(item =>
            item.StudentId == studentId
            && item.CourseId == courseId
            && item.Status == EnrollmentStatus.Active
            && item.Student.Role == UserRole.Student
            && !item.Student.IsBlocked
            && (item.Course.Status == CourseStatus.Published
                || item.Course.Status == CourseStatus.Archived),
            cancellationToken);
    }

    private async Task<(CourseworkStoredFile? File, string? Error)> TrySaveFileAsync(
        Microsoft.AspNetCore.Http.IFormFile? file,
        CancellationToken cancellationToken)
    {
        try
        {
            return (await fileStorage.SaveAsync(file, cancellationToken), null);
        }
        catch (InvalidDataException exception)
        {
            return (null, exception.Message);
        }
    }

    private async Task<CourseActionResult> SaveAsync(
        string error,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new CourseActionResult(true);
        }
        catch (DbUpdateException)
        {
            return new CourseActionResult(false, error);
        }
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime ToUtc(DateTime myt) =>
        DateTime.SpecifyKind(myt, DateTimeKind.Unspecified).AddHours(-8);

    private static DateTime ToMyt(DateTime utc) => utc.AddHours(8);
}
