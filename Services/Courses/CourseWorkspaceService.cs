using AnywhereEdureach.Models;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Services.Courses;

public sealed class CourseWorkspaceService(
    ApplicationDbContext dbContext,
    ICourseLessonFileStorage fileStorage)
    : ICourseWorkspaceService
{
    public async Task<TutorCourseWorkspaceViewModel?> GetTutorWorkspaceAsync(
        int tutorId,
        int courseId,
        CancellationToken cancellationToken)
    {
        if (!await IsAvailableTutorAsync(tutorId, cancellationToken))
        {
            return null;
        }

        return await dbContext.Courses
            .AsNoTracking()
            .Where(course => course.CourseId == courseId
                && course.TutorId == tutorId)
            .Select(course => new TutorCourseWorkspaceViewModel
            {
                CourseId = course.CourseId,
                Code = course.Code,
                Title = course.Title,
                Description = course.Description,
                ThumbnailPath = course.ThumbnailPath,
                CourseStatus = course.Status,
                RejectionReason = course.RejectionReason,
                ActiveEnrollmentCount = course.Enrollments.Count(enrollment =>
                    enrollment.Status == EnrollmentStatus.Active),
                Lessons = course.Lessons
                    .OrderByDescending(lesson => lesson.DisplayOrder)
                    .ThenByDescending(lesson => lesson.CourseLessonId)
                    .Select(lesson => new CourseLessonItemViewModel
                    {
                        CourseLessonId = lesson.CourseLessonId,
                        Title = lesson.Title,
                        Summary = lesson.Summary,
                        Content = lesson.Content,
                        ExternalResourceUrl = lesson.ExternalResourceUrl,
                        ResourceFileName = lesson.ResourceFileName,
                        ResourceSizeBytes = lesson.ResourceSizeBytes,
                        DisplayOrder = lesson.DisplayOrder,
                        IsPublished = lesson.IsPublished,
                        AvailableFromUtc = lesson.AvailableFromUtc,
                        UpdatedAtUtc = lesson.UpdatedAtUtc
                    })
                    .ToList(),
                Students = course.Enrollments
                    .Where(enrollment => enrollment.Status == EnrollmentStatus.Active)
                    .OrderBy(enrollment => enrollment.Student.Name)
                    .ThenBy(enrollment => enrollment.StudentId)
                    .Select(enrollment => new CourseStudentItemViewModel
                    {
                        StudentId = enrollment.StudentId,
                        Name = enrollment.Student.Name,
                        Email = enrollment.Student.Email,
                        PhotoPath = enrollment.Student.PhotoPath,
                        EnrolledAtUtc = enrollment.ActivatedAtUtc ?? enrollment.EnrolledAtUtc
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CourseLessonFormViewModel?> GetLessonFormAsync(
        int tutorId,
        int courseId,
        int courseLessonId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await dbContext.CourseLessons
            .AsNoTracking()
            .Where(lesson => lesson.CourseLessonId == courseLessonId
                && lesson.CourseId == courseId
                && lesson.Course.TutorId == tutorId
                && lesson.Course.Status != CourseStatus.Suspended
                && lesson.Course.Tutor.Role == UserRole.Tutor
                && !lesson.Course.Tutor.IsBlocked)
            .Select(lesson => new CourseLessonFormViewModel
            {
                CourseId = lesson.CourseId,
                CourseLessonId = lesson.CourseLessonId,
                Title = lesson.Title,
                Summary = lesson.Summary,
                Content = lesson.Content,
                ExternalResourceUrl = lesson.ExternalResourceUrl,
                ExistingResourceFileName = lesson.ResourceFileName,
                DisplayOrder = lesson.DisplayOrder,
                PublishingMode = !lesson.IsPublished
                    ? CourseLessonPublishingMode.Draft
                    : lesson.AvailableFromUtc.HasValue
                        && lesson.AvailableFromUtc.Value > now
                        ? CourseLessonPublishingMode.Schedule
                        : CourseLessonPublishingMode.PublishNow,
                AvailableFromMyt = ToMyt(lesson.AvailableFromUtc)
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CourseActionResult> CreateLessonAsync(
        int tutorId,
        CourseLessonFormViewModel model,
        CancellationToken cancellationToken)
    {
        var course = await GetEditableOwnedCourseAsync(
            tutorId,
            model.CourseId,
            cancellationToken);
        if (course is null)
        {
            return new CourseActionResult(false, "Course not found or content changes are unavailable.");
        }

        var orderAlreadyUsed = await dbContext.CourseLessons.AnyAsync(
            lesson => lesson.CourseId == model.CourseId
                && lesson.DisplayOrder == model.DisplayOrder,
            cancellationToken);
        if (orderAlreadyUsed)
        {
            return new CourseActionResult(
                false,
                "Another lesson in this course already uses this order number.",
                nameof(CourseLessonFormViewModel.DisplayOrder));
        }

        CourseLessonStoredFile? storedFile;
        try
        {
            storedFile = await fileStorage.SaveAsync(model.ResourceFile, cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            return new CourseActionResult(false, exception.Message);
        }

        if (string.IsNullOrWhiteSpace(model.Content)
            && string.IsNullOrWhiteSpace(model.ExternalResourceUrl)
            && storedFile is null)
        {
            return new CourseActionResult(
                false,
                "Add lesson text, an external resource URL, or an attachment.");
        }

        var publishing = ResolvePublishing(model);
        var now = DateTime.UtcNow;
        dbContext.CourseLessons.Add(new CourseLesson
        {
            CourseId = model.CourseId,
            Title = model.Title.Trim(),
            Summary = NullIfWhiteSpace(model.Summary),
            Content = NullIfWhiteSpace(model.Content),
            ExternalResourceUrl = NullIfWhiteSpace(model.ExternalResourceUrl),
            ResourceFileName = storedFile?.OriginalName,
            ResourceStoredName = storedFile?.StoredName,
            ResourceContentType = storedFile?.ContentType,
            ResourceSizeBytes = storedFile?.SizeBytes,
            DisplayOrder = model.DisplayOrder,
            IsPublished = publishing.IsPublished,
            AvailableFromUtc = publishing.AvailableFromUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        course.UpdatedAtUtc = now;

        var result = await SaveAsync("The lesson could not be created.", cancellationToken);
        if (!result.Succeeded)
        {
            await fileStorage.DeleteAsync(storedFile?.StoredName);
        }
        return result;
    }

    public async Task<CourseActionResult> UpdateLessonAsync(
        int tutorId,
        CourseLessonFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!model.CourseLessonId.HasValue)
        {
            return new CourseActionResult(false, "Lesson not found.");
        }

        var lesson = await dbContext.CourseLessons
            .Include(candidate => candidate.Course)
            .SingleOrDefaultAsync(candidate =>
                candidate.CourseLessonId == model.CourseLessonId.Value
                && candidate.CourseId == model.CourseId
                && candidate.Course.TutorId == tutorId
                && candidate.Course.Tutor.Role == UserRole.Tutor
                && !candidate.Course.Tutor.IsBlocked,
                cancellationToken);

        if (lesson is null || lesson.Course.Status == CourseStatus.Suspended)
        {
            return new CourseActionResult(false, "Lesson not found or content changes are unavailable.");
        }

        var orderAlreadyUsed = await dbContext.CourseLessons.AnyAsync(
            candidate => candidate.CourseId == model.CourseId
                && candidate.CourseLessonId != lesson.CourseLessonId
                && candidate.DisplayOrder == model.DisplayOrder,
            cancellationToken);
        if (orderAlreadyUsed)
        {
            return new CourseActionResult(
                false,
                "Another lesson in this course already uses this order number.",
                nameof(CourseLessonFormViewModel.DisplayOrder));
        }

        CourseLessonStoredFile? replacementFile;
        try
        {
            replacementFile = await fileStorage.SaveAsync(model.ResourceFile, cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            return new CourseActionResult(false, exception.Message);
        }

        var previousStoredName = lesson.ResourceStoredName;
        var replacesOrRemovesFile = replacementFile is not null || model.RemoveResourceFile;
        var keepsAFile = replacementFile is not null
            || (!model.RemoveResourceFile && !string.IsNullOrWhiteSpace(previousStoredName));
        if (string.IsNullOrWhiteSpace(model.Content)
            && string.IsNullOrWhiteSpace(model.ExternalResourceUrl)
            && !keepsAFile)
        {
            await fileStorage.DeleteAsync(replacementFile?.StoredName);
            return new CourseActionResult(
                false,
                "Add lesson text, an external resource URL, or an attachment.");
        }

        var publishing = ResolvePublishing(model);
        var now = DateTime.UtcNow;
        lesson.Title = model.Title.Trim();
        lesson.Summary = NullIfWhiteSpace(model.Summary);
        lesson.Content = NullIfWhiteSpace(model.Content);
        lesson.ExternalResourceUrl = NullIfWhiteSpace(model.ExternalResourceUrl);
        if (replacesOrRemovesFile)
        {
            lesson.ResourceFileName = replacementFile?.OriginalName;
            lesson.ResourceStoredName = replacementFile?.StoredName;
            lesson.ResourceContentType = replacementFile?.ContentType;
            lesson.ResourceSizeBytes = replacementFile?.SizeBytes;
        }
        lesson.DisplayOrder = model.DisplayOrder;
        lesson.IsPublished = publishing.IsPublished;
        lesson.AvailableFromUtc = publishing.AvailableFromUtc;
        lesson.UpdatedAtUtc = now;
        lesson.Course.UpdatedAtUtc = now;

        var result = await SaveAsync("The lesson could not be updated.", cancellationToken);
        if (!result.Succeeded)
        {
            await fileStorage.DeleteAsync(replacementFile?.StoredName);
            return result;
        }

        if (replacesOrRemovesFile)
        {
            await fileStorage.DeleteAsync(previousStoredName);
        }
        return result;
    }

    public async Task<CourseActionResult> DeleteLessonAsync(
        int tutorId,
        int courseId,
        int courseLessonId,
        CancellationToken cancellationToken)
    {
        var lesson = await dbContext.CourseLessons
            .Include(candidate => candidate.Course)
            .SingleOrDefaultAsync(candidate =>
                candidate.CourseLessonId == courseLessonId
                && candidate.CourseId == courseId
                && candidate.Course.TutorId == tutorId
                && candidate.Course.Tutor.Role == UserRole.Tutor
                && !candidate.Course.Tutor.IsBlocked,
                cancellationToken);

        if (lesson is null || lesson.Course.Status == CourseStatus.Suspended)
        {
            return new CourseActionResult(false, "Lesson not found or content changes are unavailable.");
        }

        var storedName = lesson.ResourceStoredName;
        lesson.Course.UpdatedAtUtc = DateTime.UtcNow;
        dbContext.CourseLessons.Remove(lesson);
        var result = await SaveAsync("The lesson could not be removed.", cancellationToken);
        if (result.Succeeded)
        {
            await fileStorage.DeleteAsync(storedName);
        }
        return result;
    }

    private Task<Course?> GetEditableOwnedCourseAsync(
        int tutorId,
        int courseId,
        CancellationToken cancellationToken)
    {
        return dbContext.Courses.SingleOrDefaultAsync(
            course => course.CourseId == courseId
                && course.TutorId == tutorId
                && course.Status != CourseStatus.Suspended,
            cancellationToken);
    }

    private Task<bool> IsAvailableTutorAsync(
        int tutorId,
        CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(
            user => user.Id == tutorId
                && user.Role == UserRole.Tutor
                && !user.IsBlocked,
            cancellationToken);
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

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime? ToUtc(DateTime? myt)
    {
        return myt.HasValue
            ? DateTime.SpecifyKind(myt.Value, DateTimeKind.Unspecified).AddHours(-8)
            : null;
    }

    private static DateTime? ToMyt(DateTime? utc)
    {
        return utc?.AddHours(8);
    }

    private static (bool IsPublished, DateTime? AvailableFromUtc) ResolvePublishing(
        CourseLessonFormViewModel model)
    {
        return model.PublishingMode switch
        {
            CourseLessonPublishingMode.PublishNow => (true, null),
            CourseLessonPublishingMode.Schedule => (true, ToUtc(model.AvailableFromMyt)),
            _ => (false, null)
        };
    }
}
