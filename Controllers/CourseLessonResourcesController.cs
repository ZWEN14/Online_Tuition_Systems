using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.Services.Courses;

namespace Online_Tuition_Systems.Controllers;

[Authorize(Roles = AppRoles.StudentOrTutor)]
public sealed class CourseLessonResourcesController(
    ApplicationDbContext dbContext,
    ICourseLessonFileStorage fileStorage) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Download(
        int id,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Challenge();
        }

        var now = DateTime.UtcNow;
        var resource = await dbContext.CourseLessons
            .AsNoTracking()
            .Where(lesson => lesson.CourseLessonId == id
                && lesson.ResourceStoredName != null
                && lesson.ResourceFileName != null
                && lesson.ResourceContentType != null)
            .Select(lesson => new
            {
                lesson.CourseLessonId,
                lesson.ResourceStoredName,
                lesson.ResourceFileName,
                lesson.ResourceContentType,
                lesson.IsPublished,
                lesson.AvailableFromUtc,
                lesson.CourseId,
                lesson.Course.TutorId,
                CourseStatus = lesson.Course.Status
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (resource is null)
        {
            return NotFound();
        }

        var userAvailable = await dbContext.Users.AsNoTracking().AnyAsync(
            user => user.Id == userId && !user.IsBlocked,
            cancellationToken);
        if (!userAvailable)
        {
            return Forbid();
        }

        var tutorOwnsCourse = User.IsInRole(AppRoles.Tutor)
            && resource.TutorId == userId;
        var studentHasAccess = User.IsInRole(AppRoles.Student)
            && resource.IsPublished
            && (resource.AvailableFromUtc is null || resource.AvailableFromUtc <= now)
            && resource.CourseStatus is CourseStatus.Published or CourseStatus.Archived
            && await dbContext.Enrollments.AsNoTracking().AnyAsync(
                enrollment => enrollment.StudentId == userId
                    && enrollment.CourseId == resource.CourseId
                    && enrollment.Status == EnrollmentStatus.Active,
                cancellationToken);

        if (!tutorOwnsCourse && !studentHasAccess)
        {
            return NotFound();
        }

        Stream? stream;
        try
        {
            stream = fileStorage.OpenRead(resource.ResourceStoredName!);
        }
        catch (InvalidDataException)
        {
            return NotFound();
        }

        return stream is null
            ? NotFound()
            : File(
                stream,
                resource.ResourceContentType!,
                resource.ResourceFileName!,
                enableRangeProcessing: true);
    }
}
