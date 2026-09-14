using System.Globalization;
using AnywhereEdureach.Models;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Services.Courses;

public sealed class CourseStreamService(ApplicationDbContext dbContext)
    : ICourseStreamService
{
    private static readonly TimeSpan CommentEditWindow = TimeSpan.FromMinutes(10);

    public async Task<CourseStreamViewModel?> GetForTutorAsync(
        int tutorId,
        int courseId,
        string? sort,
        CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses
            .AsNoTracking()
            .Where(item => item.CourseId == courseId
                && item.TutorId == tutorId
                && item.Tutor.Role == UserRole.Tutor
                && !item.Tutor.IsBlocked)
            .Select(item => new { item.Status, item.Tutor.Name, item.Tutor.PhotoPath })
            .SingleOrDefaultAsync(cancellationToken);

        return course is null
            ? null
            : await BuildAsync(
                courseId,
                tutorId,
                isTutor: true,
                canContribute: course.Status == CourseStatus.Published,
                tutorName: course.Name,
                tutorPhotoPath: course.PhotoPath,
                includeTutorAudienceEvents: true,
                sort: sort,
                cancellationToken: cancellationToken);
    }

    public async Task<CourseStreamViewModel?> GetForStudentAsync(
        int studentId,
        int courseId,
        string? sort,
        CancellationToken cancellationToken)
    {
        var access = await dbContext.Enrollments
            .AsNoTracking()
            .Where(item => item.CourseId == courseId
                && item.StudentId == studentId
                && item.Status == EnrollmentStatus.Active
                && item.Student.Role == UserRole.Student
                && !item.Student.IsBlocked
                && (item.Course.Status == CourseStatus.Published
                    || item.Course.Status == CourseStatus.Archived))
            .Select(item => new
            {
                item.Course.Status,
                item.Course.Tutor.Name,
                item.Course.Tutor.PhotoPath
            })
            .SingleOrDefaultAsync(cancellationToken);

        return access is null
            ? null
            : await BuildAsync(
                courseId,
                studentId,
                isTutor: false,
                canContribute: access.Status == CourseStatus.Published,
                tutorName: access.Name,
                tutorPhotoPath: access.PhotoPath,
                includeTutorAudienceEvents: false,
                sort: sort,
                cancellationToken: cancellationToken);
    }

    public async Task<CourseActionResult> CreatePostAsync(
        int tutorId,
        CourseStreamPostFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!await CanTutorContributeAsync(tutorId, model.CourseId, cancellationToken))
        {
            return new CourseActionResult(false, "Course posts are available only for your published course.");
        }

        var now = DateTimeOffset.UtcNow;
        dbContext.Announcements.Add(new Announcement
        {
            CourseId = model.CourseId,
            CreatedByUserId = tutorId.ToString(CultureInfo.InvariantCulture),
            Title = model.Title.Trim(),
            Content = model.Content.Trim(),
            Audience = AnnouncementAudience.Student,
            Priority = AnnouncementPriority.Normal,
            Status = AnnouncementStatus.Published,
            PublishedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });

        return await SaveAsync("The Course post could not be published.", cancellationToken);
    }

    public Task<CourseStreamPostFormViewModel?> GetPostFormAsync(
        int tutorId,
        int courseId,
        int announcementId,
        CancellationToken cancellationToken)
    {
        var tutorReference = tutorId.ToString(CultureInfo.InvariantCulture);
        return dbContext.Announcements
            .AsNoTracking()
            .Where(item => item.Id == announcementId
                && item.CourseId == courseId
                && item.EventId == null
                && item.CreatedByUserId == tutorReference
                && item.CourseId != null
                && dbContext.Courses.Any(course => course.CourseId == item.CourseId
                    && course.TutorId == tutorId
                    && course.Status == CourseStatus.Published))
            .Select(item => new CourseStreamPostFormViewModel
            {
                CourseId = courseId,
                AnnouncementId = item.Id,
                Title = item.Title,
                Content = item.Content
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CourseActionResult> UpdatePostAsync(
        int tutorId,
        CourseStreamPostFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!model.AnnouncementId.HasValue
            || !await CanTutorContributeAsync(tutorId, model.CourseId, cancellationToken))
        {
            return new CourseActionResult(false, "Course post not found or the Stream is read-only.");
        }

        var tutorReference = tutorId.ToString(CultureInfo.InvariantCulture);
        var post = await dbContext.Announcements.SingleOrDefaultAsync(item =>
            item.Id == model.AnnouncementId.Value
            && item.CourseId == model.CourseId
            && item.EventId == null
            && item.CreatedByUserId == tutorReference,
            cancellationToken);
        if (post is null)
        {
            return new CourseActionResult(false, "Course post not found.");
        }

        post.Title = model.Title.Trim();
        post.Content = model.Content.Trim();
        post.UpdatedAt = DateTimeOffset.UtcNow;
        return await SaveAsync("The Course post could not be updated.", cancellationToken);
    }

    public async Task<CourseActionResult> DeletePostAsync(
        int tutorId,
        int courseId,
        int announcementId,
        CancellationToken cancellationToken)
    {
        if (!await CanTutorContributeAsync(tutorId, courseId, cancellationToken))
        {
            return new CourseActionResult(false, "Course post not found or the Stream is read-only.");
        }

        var tutorReference = tutorId.ToString(CultureInfo.InvariantCulture);
        var post = await dbContext.Announcements.SingleOrDefaultAsync(item =>
            item.Id == announcementId
            && item.CourseId == courseId
            && item.EventId == null
            && item.CreatedByUserId == tutorReference,
            cancellationToken);
        if (post is null)
        {
            return new CourseActionResult(false, "Course post not found.");
        }

        dbContext.Announcements.Remove(post);
        return await SaveAsync("The Course post could not be removed.", cancellationToken);
    }

    public async Task<CourseActionResult> AddCommentAsync(
        int userId,
        bool isTutor,
        CourseStreamCommentFormViewModel model,
        CancellationToken cancellationToken)
    {
        var hasAccess = isTutor
            ? await CanTutorContributeAsync(userId, model.CourseId, cancellationToken)
            : await CanStudentContributeAsync(userId, model.CourseId, cancellationToken);
        if (!hasAccess)
        {
            return new CourseActionResult(false, "Comments are unavailable for this Course.");
        }

        var now = DateTimeOffset.UtcNow;
        var postExists = await dbContext.Announcements.AnyAsync(item =>
            item.Id == model.AnnouncementId
            && item.CourseId == model.CourseId
            && item.EventId == null
            && item.Status == AnnouncementStatus.Published
            && item.PublishedAt.HasValue
            && item.PublishedAt.Value <= now
            && (!item.ExpiresAt.HasValue || item.ExpiresAt.Value > now),
            cancellationToken);
        if (!postExists)
        {
            return new CourseActionResult(false, "Course post not found.");
        }

        var utcNow = DateTime.UtcNow;
        dbContext.CourseStreamComments.Add(new CourseStreamComment
        {
            AnnouncementId = model.AnnouncementId,
            UserId = userId,
            Content = model.Content.Trim(),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        });
        return await SaveAsync("The comment could not be added.", cancellationToken);
    }

    public async Task<CourseActionResult> DeleteCommentAsync(
        int tutorId,
        int courseId,
        int commentId,
        CancellationToken cancellationToken)
    {
        var tutorName = await dbContext.Courses
            .Where(item => item.CourseId == courseId
                && item.TutorId == tutorId
                && item.Status == CourseStatus.Published
                && item.Tutor.Role == UserRole.Tutor
                && !item.Tutor.IsBlocked)
            .Select(item => item.Tutor.Name)
            .SingleOrDefaultAsync(cancellationToken);
        if (tutorName is null)
        {
            return new CourseActionResult(false, "You cannot moderate comments for this Course.");
        }

        var comment = await dbContext.CourseStreamComments
            .Include(item => item.Announcement)
            .SingleOrDefaultAsync(item => item.CourseStreamCommentId == commentId
                && item.Announcement.CourseId == courseId
                && item.Announcement.EventId == null,
                cancellationToken);
        if (comment is null)
        {
            return new CourseActionResult(false, "Comment not found.");
        }

        if (comment.IsRemovedByTutor)
        {
            return new CourseActionResult(false, "This comment has already been moderated.");
        }

        var utcNow = DateTime.UtcNow;
        comment.IsRemovedByTutor = true;
        comment.RemovedAtUtc = utcNow;
        comment.RemovedByTutorName = tutorName;
        comment.UpdatedAtUtc = utcNow;
        return await SaveAsync("The comment could not be moderated.", cancellationToken);
    }

    public async Task<CourseStreamCommentEditViewModel?> GetCommentFormAsync(
        int userId,
        bool isTutor,
        int courseId,
        int commentId,
        CancellationToken cancellationToken)
    {
        if (!await CanUserContributeAsync(userId, isTutor, courseId, cancellationToken))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var editableSinceUtc = now.UtcDateTime.Subtract(CommentEditWindow);
        return await dbContext.CourseStreamComments
            .AsNoTracking()
            .Where(item => item.CourseStreamCommentId == commentId
                && item.UserId == userId
                && !item.IsRemovedByTutor
                && item.CreatedAtUtc >= editableSinceUtc
                && item.Announcement.CourseId == courseId
                && item.Announcement.EventId == null
                && item.Announcement.Status == AnnouncementStatus.Published
                && item.Announcement.PublishedAt.HasValue
                && item.Announcement.PublishedAt.Value <= now
                && (!item.Announcement.ExpiresAt.HasValue
                    || item.Announcement.ExpiresAt.Value > now))
            .Select(item => new CourseStreamCommentEditViewModel
            {
                CourseId = courseId,
                CourseStreamCommentId = item.CourseStreamCommentId,
                Content = item.Content
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CourseActionResult> UpdateCommentAsync(
        int userId,
        bool isTutor,
        CourseStreamCommentEditViewModel model,
        CancellationToken cancellationToken)
    {
        if (!await CanUserContributeAsync(userId, isTutor, model.CourseId, cancellationToken))
        {
            return new CourseActionResult(false, "Comments are unavailable for this Course.");
        }

        var now = DateTimeOffset.UtcNow;
        var editableSinceUtc = now.UtcDateTime.Subtract(CommentEditWindow);
        var comment = await dbContext.CourseStreamComments
            .Include(item => item.Announcement)
            .SingleOrDefaultAsync(item =>
                item.CourseStreamCommentId == model.CourseStreamCommentId
                && item.UserId == userId
                && !item.IsRemovedByTutor
                && item.CreatedAtUtc >= editableSinceUtc
                && item.Announcement.CourseId == model.CourseId
                && item.Announcement.EventId == null
                && item.Announcement.Status == AnnouncementStatus.Published
                && item.Announcement.PublishedAt.HasValue
                && item.Announcement.PublishedAt.Value <= now
                && (!item.Announcement.ExpiresAt.HasValue
                    || item.Announcement.ExpiresAt.Value > now),
                cancellationToken);
        if (comment is null)
        {
            return new CourseActionResult(false, "This comment can no longer be edited.");
        }

        comment.Content = model.Content.Trim();
        comment.UpdatedAtUtc = DateTime.UtcNow;
        return await SaveAsync("The comment could not be updated.", cancellationToken);
    }

    private async Task<CourseStreamViewModel> BuildAsync(
        int courseId,
        int currentUserId,
        bool isTutor,
        bool canContribute,
        string tutorName,
        string? tutorPhotoPath,
        bool includeTutorAudienceEvents,
        string? sort,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var normalizedSort = CourseStreamSortOptions.Normalize(sort);
        var eventsQuery = dbContext.Events.AsNoTracking().Where(item =>
            item.CourseId == courseId
            && item.Status == EventStatus.Published
            && item.EndsAt >= now);
        if (!includeTutorAudienceEvents)
        {
            eventsQuery = eventsQuery.Where(item =>
                item.RegistrationAudience == RegistrationAudience.All
                || item.RegistrationAudience == RegistrationAudience.Student);
        }

        var events = await eventsQuery
            .OrderBy(item => item.StartsAt)
            .Take(5)
            .Select(item => new CourseStreamEventViewModel
            {
                EventId = item.Id,
                Title = item.Title,
                Mode = item.Mode,
                StartsAt = item.StartsAt,
                EndsAt = item.EndsAt
            })
            .ToListAsync(cancellationToken);

        var postsQuery = dbContext.Announcements
            .AsNoTracking()
            .Where(item => item.CourseId == courseId
                && item.EventId == null
                && item.Status == AnnouncementStatus.Published
                && item.PublishedAt.HasValue
                && item.PublishedAt.Value <= now
                && (!item.ExpiresAt.HasValue || item.ExpiresAt.Value > now));

        postsQuery = normalizedSort == CourseStreamSortOptions.Oldest
            ? postsQuery.OrderBy(item => item.PublishedAt).ThenBy(item => item.Id)
            : postsQuery.OrderByDescending(item => item.PublishedAt).ThenByDescending(item => item.Id);

        var commentEditableSinceUtc = DateTime.UtcNow.Subtract(CommentEditWindow);
        var posts = await postsQuery
            .Select(item => new CourseStreamPostViewModel
            {
                AnnouncementId = item.Id,
                Title = item.Title,
                Content = item.Content,
                TutorName = tutorName,
                TutorPhotoPath = tutorPhotoPath,
                PublishedAt = item.PublishedAt!.Value,
                UpdatedAt = item.UpdatedAt,
                Comments = dbContext.CourseStreamComments
                    .Where(comment => comment.AnnouncementId == item.Id)
                    .OrderBy(comment => comment.CreatedAtUtc)
                    .Select(comment => new CourseStreamCommentViewModel
                    {
                        CourseStreamCommentId = comment.CourseStreamCommentId,
                        UserId = comment.UserId,
                        UserName = comment.User.Name,
                        UserPhotoPath = comment.User.PhotoPath,
                        Content = comment.IsRemovedByTutor ? string.Empty : comment.Content,
                        CreatedAtUtc = comment.CreatedAtUtc,
                        UpdatedAtUtc = comment.UpdatedAtUtc,
                        IsRemovedByTutor = comment.IsRemovedByTutor,
                        RemovedAtUtc = comment.RemovedAtUtc,
                        RemovedByTutorName = comment.RemovedByTutorName,
                        CanEdit = canContribute
                            && !comment.IsRemovedByTutor
                            && comment.UserId == currentUserId
                            && comment.CreatedAtUtc >= commentEditableSinceUtc
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return new CourseStreamViewModel
        {
            CourseId = courseId,
            IsTutor = isTutor,
            CurrentUserId = currentUserId,
            CanContribute = canContribute,
            Sort = normalizedSort,
            UpcomingEvents = events,
            Posts = posts
        };
    }

    private Task<bool> CanTutorContributeAsync(
        int tutorId,
        int courseId,
        CancellationToken cancellationToken)
    {
        return dbContext.Courses.AnyAsync(item => item.CourseId == courseId
            && item.TutorId == tutorId
            && item.Status == CourseStatus.Published
            && item.Tutor.Role == UserRole.Tutor
            && !item.Tutor.IsBlocked,
            cancellationToken);
    }

    private Task<bool> CanStudentContributeAsync(
        int studentId,
        int courseId,
        CancellationToken cancellationToken)
    {
        return dbContext.Enrollments.AnyAsync(item => item.CourseId == courseId
            && item.StudentId == studentId
            && item.Status == EnrollmentStatus.Active
            && item.Course.Status == CourseStatus.Published
            && item.Student.Role == UserRole.Student
            && !item.Student.IsBlocked,
            cancellationToken);
    }

    private Task<bool> CanUserContributeAsync(
        int userId,
        bool isTutor,
        int courseId,
        CancellationToken cancellationToken)
    {
        return isTutor
            ? CanTutorContributeAsync(userId, courseId, cancellationToken)
            : CanStudentContributeAsync(userId, courseId, cancellationToken);
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
}
