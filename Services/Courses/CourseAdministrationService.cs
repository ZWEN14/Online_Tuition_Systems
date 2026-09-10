using System.Linq.Expressions;
using AnywhereEdureach.Models;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Services.Courses;

public sealed class CourseAdministrationService(ApplicationDbContext dbContext)
    : ICourseAdministrationService
{
    public async Task<IReadOnlyList<CourseCategoryListItemViewModel>> GetCategoriesAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.CourseCategories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CourseCategoryListItemViewModel
            {
                CourseCategoryId = category.CourseCategoryId,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                CourseCount = category.Courses.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CourseActionResult> CreateCategoryAsync(
        CourseCategoryFormViewModel model,
        CancellationToken cancellationToken)
    {
        var normalizedName = model.Name.Trim();
        if (await dbContext.CourseCategories.AnyAsync(
            category => category.Name == normalizedName,
            cancellationToken))
        {
            return new CourseActionResult(false, "A category with this name already exists.");
        }

        var now = DateTime.UtcNow;
        dbContext.CourseCategories.Add(new CourseCategory
        {
            Name = normalizedName,
            Description = string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim(),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new CourseActionResult(false, "The category could not be created.");
        }

        return new CourseActionResult(true);
    }

    public async Task<CourseActionResult> ToggleCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.CourseCategories.FindAsync(
            new object[] { categoryId },
            cancellationToken);

        if (category is null)
        {
            return new CourseActionResult(false, "Category not found.");
        }

        category.IsActive = !category.IsActive;
        category.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CourseActionResult(true);
    }

    public async Task<IReadOnlyList<AdminCourseReviewViewModel>> GetPendingReviewsAsync(
        CancellationToken cancellationToken)
    {
        return await PendingReviewQuery()
            .OrderBy(course => course.UpdatedAtUtc)
            .Select(ReviewProjection)
            .ToListAsync(cancellationToken);
    }

    public Task<AdminCourseReviewViewModel?> GetPendingReviewAsync(
        int courseId,
        CancellationToken cancellationToken)
    {
        return PendingReviewQuery()
            .Where(course => course.CourseId == courseId)
            .Select(ReviewProjection)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CourseActionResult> ApproveAsync(
        int administratorId,
        int courseId,
        CancellationToken cancellationToken)
    {
        var course = await GetCourseForReviewAsync(
            administratorId,
            courseId,
            cancellationToken);

        if (course is null)
        {
            return new CourseActionResult(false, "The pending course was not found.");
        }

        var categoryIsActive = await dbContext.CourseCategories.AnyAsync(
            category => category.CourseCategoryId == course.CourseCategoryId
                && category.IsActive,
            cancellationToken);

        if (!categoryIsActive)
        {
            return new CourseActionResult(
                false,
                "Reactivate the course category before approving this course.");
        }

        var now = DateTime.UtcNow;
        course.Status = CourseStatus.Published;
        course.ReviewedByUserId = administratorId;
        course.ReviewedAtUtc = now;
        course.PublishedAtUtc = now;
        course.RejectionReason = null;
        course.UpdatedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new CourseActionResult(true);
    }

    public async Task<CourseActionResult> RejectAsync(
        int administratorId,
        int courseId,
        string reason,
        CancellationToken cancellationToken)
    {
        var course = await GetCourseForReviewAsync(
            administratorId,
            courseId,
            cancellationToken);

        if (course is null)
        {
            return new CourseActionResult(false, "The pending course was not found.");
        }

        var now = DateTime.UtcNow;
        course.Status = CourseStatus.Rejected;
        course.ReviewedByUserId = administratorId;
        course.ReviewedAtUtc = now;
        course.PublishedAtUtc = null;
        course.RejectionReason = reason.Trim();
        course.UpdatedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new CourseActionResult(true);
    }

    public async Task<IReadOnlyList<AdminCourseManagementViewModel>> GetCoursesAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Courses
            .AsNoTracking()
            .OrderByDescending(course => course.UpdatedAtUtc)
            .Select(course => new AdminCourseManagementViewModel
            {
                CourseId = course.CourseId,
                Code = course.Code,
                Title = course.Title,
                TutorName = course.Tutor.Name,
                CategoryName = course.Category.Name,
                Status = course.Status,
                SuspensionReason = course.SuspensionReason,
                UpdatedAtUtc = course.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public Task<AdminCourseManagementViewModel?> GetCourseAsync(
        int courseId,
        CancellationToken cancellationToken)
    {
        return dbContext.Courses
            .AsNoTracking()
            .Where(course => course.CourseId == courseId)
            .Select(course => new AdminCourseManagementViewModel
            {
                CourseId = course.CourseId,
                Code = course.Code,
                Title = course.Title,
                TutorName = course.Tutor.Name,
                CategoryName = course.Category.Name,
                Status = course.Status,
                SuspensionReason = course.SuspensionReason,
                UpdatedAtUtc = course.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CourseActionResult> SuspendAsync(
        int administratorId,
        int courseId,
        string reason,
        CancellationToken cancellationToken)
    {
        if (!await AdministratorIsActiveAsync(administratorId, cancellationToken))
        {
            return new CourseActionResult(false, "Administrator account is not available.");
        }

        var course = await dbContext.Courses.SingleOrDefaultAsync(
            candidate => candidate.CourseId == courseId,
            cancellationToken);

        if (course is null
            || course.Status is not CourseStatus.Published and not CourseStatus.Archived)
        {
            return new CourseActionResult(false, "Only published or archived courses can be suspended.");
        }

        course.StatusBeforeSuspension = course.Status;
        course.Status = CourseStatus.Suspended;
        course.SuspensionReason = reason.Trim();
        course.ReviewedByUserId = administratorId;
        course.ReviewedAtUtc = DateTime.UtcNow;
        course.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new CourseActionResult(true);
    }

    public async Task<CourseActionResult> RestoreAsync(
        int administratorId,
        int courseId,
        CancellationToken cancellationToken)
    {
        if (!await AdministratorIsActiveAsync(administratorId, cancellationToken))
        {
            return new CourseActionResult(false, "Administrator account is not available.");
        }

        var course = await dbContext.Courses.SingleOrDefaultAsync(
            candidate => candidate.CourseId == courseId,
            cancellationToken);

        if (course is null || course.Status != CourseStatus.Suspended)
        {
            return new CourseActionResult(false, "Only a suspended course can be restored.");
        }

        course.Status = course.StatusBeforeSuspension is CourseStatus.Published or CourseStatus.Archived
            ? course.StatusBeforeSuspension.Value
            : CourseStatus.Archived;
        course.StatusBeforeSuspension = null;
        course.SuspensionReason = null;
        course.ReviewedByUserId = administratorId;
        course.ReviewedAtUtc = DateTime.UtcNow;
        course.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new CourseActionResult(true);
    }

    private IQueryable<Course> PendingReviewQuery()
    {
        return dbContext.Courses
            .AsNoTracking()
            .Where(course => course.Status == CourseStatus.PendingReview);
    }

    private async Task<Course?> GetCourseForReviewAsync(
        int administratorId,
        int courseId,
        CancellationToken cancellationToken)
    {
        if (!await AdministratorIsActiveAsync(administratorId, cancellationToken))
        {
            return null;
        }

        return await dbContext.Courses.SingleOrDefaultAsync(
            course => course.CourseId == courseId
                && course.Status == CourseStatus.PendingReview,
            cancellationToken);
    }

    private Task<bool> AdministratorIsActiveAsync(
        int administratorId,
        CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(
            user => user.Id == administratorId
                && user.Role == UserRole.Admin
                && !user.IsBlocked,
            cancellationToken);
    }

    private static Expression<Func<Course, AdminCourseReviewViewModel>> ReviewProjection =>
        course => new AdminCourseReviewViewModel
        {
            CourseId = course.CourseId,
            Code = course.Code,
            Title = course.Title,
            TutorName = course.Tutor.Name,
            CategoryName = course.Category.Name,
            ShortDescription = course.ShortDescription,
            Description = course.Description,
            Price = course.Price,
            SubmittedAtUtc = course.UpdatedAtUtc
        };
}
