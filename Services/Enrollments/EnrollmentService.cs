using AnywhereEdureach.Models;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.ViewModels.Enrollments;

namespace Online_Tuition_Systems.Services.Enrollments;

public sealed class EnrollmentService(ApplicationDbContext dbContext) : IEnrollmentService
{
    public Task<EnrollmentStatus?> GetStatusAsync(
        int studentId,
        int courseId,
        CancellationToken cancellationToken)
    {
        return dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.StudentId == studentId
                && enrollment.CourseId == courseId)
            .Select(enrollment => (EnrollmentStatus?)enrollment.Status)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<EnrollmentActionResult> EnrollAsync(
        int studentId,
        int courseId,
        CancellationToken cancellationToken)
    {
        if (!await IsAvailableStudentAsync(studentId, cancellationToken))
        {
            return new EnrollmentActionResult(
                false,
                Message: "Your Student account is not available.");
        }

        var now = DateTime.UtcNow;
        var course = await dbContext.Courses
            .AsNoTracking()
            .Where(candidate => candidate.CourseId == courseId)
            .Select(candidate => new
            {
                candidate.CourseId,
                candidate.TutorId,
                candidate.Title,
                candidate.Price,
                candidate.Status,
                candidate.PublishedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (course is null
            || course.Status != CourseStatus.Published
            || course.PublishedAtUtc is null
            || course.PublishedAtUtc > now)
        {
            return new EnrollmentActionResult(
                false,
                Message: "This course is not available for enrollment.");
        }

        var existingStatus = await GetStatusAsync(
            studentId,
            courseId,
            cancellationToken);

        if (existingStatus.HasValue)
        {
            return ExistingEnrollmentResult(existingStatus.Value);
        }

        var isFree = course.Price == 0;
        dbContext.Enrollments.Add(new Enrollment
        {
            StudentId = studentId,
            CourseId = course.CourseId,
            Status = isFree
                ? EnrollmentStatus.Active
                : EnrollmentStatus.PendingPayment,
            EnrolledAtUtc = now,
            ActivatedAtUtc = isFree ? now : null
        });
        dbContext.Notifications.Add(new UserNotification
        {
            UserId = studentId, Type = UserNotificationType.EnrollmentCreated,
            Title = isFree ? "Course enrollment active" : "Course enrollment pending payment",
            Message = isFree ? $"You are enrolled in '{course.Title}'." : $"Complete payment to access '{course.Title}'.",
            TargetUrl = "/StudentCourses/Index"
        });
        dbContext.Notifications.Add(new UserNotification
        {
            UserId = course.TutorId, Type = UserNotificationType.EnrollmentCreated,
            Title = "New course enrollment", Message = $"A student enrolled in '{course.Title}'.",
            TargetUrl = "/TutorCourses/Index"
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            var concurrentStatus = await GetStatusAsync(
                studentId,
                courseId,
                cancellationToken);

            return concurrentStatus.HasValue
                ? ExistingEnrollmentResult(concurrentStatus.Value)
                : new EnrollmentActionResult(
                    false,
                    Message: "Enrollment could not be completed. Please try again.");
        }

        return isFree
            ? new EnrollmentActionResult(
                true,
                Message: $"You are now enrolled in {course.Title}.")
            : new EnrollmentActionResult(
                true,
                RequiresPayment: true,
                Message: $"Enrollment for {course.Title} is pending payment.");
    }

    public async Task<StudentCoursesResult> GetStudentCoursesAsync(
        int studentId,
        CancellationToken cancellationToken)
    {
        if (!await IsAvailableStudentAsync(studentId, cancellationToken))
        {
            return new StudentCoursesResult(false);
        }

        var courses = await dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.StudentId == studentId)
            .OrderByDescending(enrollment => enrollment.EnrolledAtUtc)
            .Select(enrollment => new StudentCourseListItemViewModel
            {
                EnrollmentId = enrollment.EnrollmentId,
                CourseId = enrollment.CourseId,
                Code = enrollment.Course.Code,
                Title = enrollment.Course.Title,
                TutorName = enrollment.Course.Tutor.Name,
                ThumbnailPath = enrollment.Course.ThumbnailPath,
                Price = enrollment.Course.Price,
                EnrollmentStatus = enrollment.Status,
                CourseStatus = enrollment.Course.Status,
                EnrolledAtUtc = enrollment.EnrolledAtUtc,
                HasAccess = enrollment.Status == EnrollmentStatus.Active
                    && (enrollment.Course.Status == CourseStatus.Published
                        || enrollment.Course.Status == CourseStatus.Archived)
            })
            .ToListAsync(cancellationToken);

        return new StudentCoursesResult(
            true,
            new StudentCoursesViewModel { Courses = courses });
    }

    public async Task<CourseAccessResult> GetCourseAccessAsync(
        int studentId,
        int courseId,
        CancellationToken cancellationToken)
    {
        if (!await IsAvailableStudentAsync(studentId, cancellationToken))
        {
            return new CourseAccessResult(
                false,
                Error: "Your Student account is not available.");
        }

        var enrollment = await dbContext.Enrollments
            .AsNoTracking()
            .Where(candidate => candidate.StudentId == studentId
                && candidate.CourseId == courseId)
            .Select(candidate => new
            {
                candidate.Status,
                Course = new CourseAccessViewModel
                {
                    CourseId = candidate.Course.CourseId,
                    Code = candidate.Course.Code,
                    Title = candidate.Course.Title,
                    TutorName = candidate.Course.Tutor.Name,
                    Description = candidate.Course.Description,
                    ThumbnailPath = candidate.Course.ThumbnailPath,
                    CourseStatus = candidate.Course.Status
                }
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (enrollment is null)
        {
            return new CourseAccessResult(
                false,
                Error: "Enroll in this course before opening it.");
        }

        if (enrollment.Status != EnrollmentStatus.Active)
        {
            return new CourseAccessResult(
                false,
                Error: "Course access is available after enrollment payment is completed.");
        }

        if (enrollment.Course.CourseStatus == CourseStatus.Suspended)
        {
            return new CourseAccessResult(
                false,
                Error: "This course is temporarily unavailable because it has been suspended.");
        }

        if (enrollment.Course.CourseStatus is not CourseStatus.Published
            and not CourseStatus.Archived)
        {
            return new CourseAccessResult(
                false,
                Error: "This course is not currently available.");
        }

        return new CourseAccessResult(true, enrollment.Course);
    }

    private Task<bool> IsAvailableStudentAsync(
        int studentId,
        CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(
            user => user.Id == studentId
                && user.Role == UserRole.Student
                && !user.IsBlocked,
            cancellationToken);
    }

    private static EnrollmentActionResult ExistingEnrollmentResult(
        EnrollmentStatus status)
    {
        return status switch
        {
            EnrollmentStatus.Active => new EnrollmentActionResult(
                true,
                Message: "You are already enrolled in this course."),
            EnrollmentStatus.PendingPayment => new EnrollmentActionResult(
                true,
                RequiresPayment: true,
                Message: "Your enrollment is already pending payment."),
            _ => new EnrollmentActionResult(
                false,
                Message: "A cancelled enrollment cannot be reopened. Please contact an administrator.")
        };
    }
}
