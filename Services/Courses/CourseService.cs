using System.Text.RegularExpressions;
using AnywhereEdureach.Models;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.ViewModels.Courses;

namespace Online_Tuition_Systems.Services.Courses;

public sealed partial class CourseService(
    ApplicationDbContext dbContext,
    ILocalCourseImageStorage imageStorage) : ICourseService
{
    private const int PageSize = 9;

    public async Task<CourseCatalogViewModel> GetPublishedAsync(
        CourseCatalogViewModel query,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var page = Math.Max(query.Page, 1);
        var search = query.Search?.Trim();

        var courses = dbContext.Courses
            .AsNoTracking()
            .Where(course => course.Status == CourseStatus.Published
                && course.PublishedAtUtc != null
                && course.PublishedAtUtc <= now);

        if (!string.IsNullOrWhiteSpace(search))
        {
            courses = courses.Where(course =>
                course.Title.Contains(search)
                || course.Code.Contains(search)
                || (course.ShortDescription != null
                    && course.ShortDescription.Contains(search)));
        }

        if (query.CategoryId.HasValue)
        {
            courses = courses.Where(course =>
                course.CourseCategoryId == query.CategoryId.Value);
        }

        var totalCourses = await courses.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCourses / (double)PageSize));
        page = Math.Min(page, totalPages);

        var items = await courses
            .OrderByDescending(course => course.PublishedAtUtc)
            .ThenBy(course => course.Title)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(course => new CourseListItemViewModel
            {
                CourseId = course.CourseId,
                Code = course.Code,
                Title = course.Title,
                Slug = course.Slug,
                CategoryName = course.Category.Name,
                TutorName = course.Tutor.Name,
                ShortDescription = course.ShortDescription,
                ThumbnailPath = course.ThumbnailPath,
                Price = course.Price
            })
            .ToListAsync(cancellationToken);

        return new CourseCatalogViewModel
        {
            Search = search,
            CategoryId = query.CategoryId,
            Page = page,
            TotalPages = totalPages,
            TotalCourses = totalCourses,
            Categories = await GetActiveCategoriesAsync(cancellationToken),
            Courses = items
        };
    }

    public Task<CourseDetailsViewModel?> GetPublishedDetailsAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        return dbContext.Courses
            .AsNoTracking()
            .Where(course => course.Slug == slug
                && course.Status == CourseStatus.Published
                && course.PublishedAtUtc != null
                && course.PublishedAtUtc <= now)
            .Select(course => new CourseDetailsViewModel
            {
                CourseId = course.CourseId,
                Code = course.Code,
                Title = course.Title,
                CategoryName = course.Category.Name,
                TutorName = course.Tutor.Name,
                Description = course.Description,
                ThumbnailPath = course.ThumbnailPath,
                Price = course.Price,
                PublishedAtUtc = course.PublishedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TutorCourseListItemViewModel>> GetTutorCoursesAsync(
        int tutorId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Courses
            .AsNoTracking()
            .Where(course => course.TutorId == tutorId)
            .OrderByDescending(course => course.UpdatedAtUtc)
            .Select(course => new TutorCourseListItemViewModel
            {
                CourseId = course.CourseId,
                Code = course.Code,
                Title = course.Title,
                CategoryName = course.Category.Name,
                Price = course.Price,
                Status = course.Status,
                RejectionReason = course.RejectionReason,
                UpdatedAtUtc = course.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task PopulateCategoriesAsync(
        CourseFormViewModel model,
        CancellationToken cancellationToken)
    {
        model.Categories = await GetActiveCategoriesAsync(cancellationToken);
    }

    public async Task<CourseCreateResult> CreateDraftAsync(
        int tutorId,
        CourseFormViewModel model,
        CancellationToken cancellationToken)
    {
        var tutorExists = await dbContext.Users.AnyAsync(
            user => user.Id == tutorId
                && user.Role == UserRole.Tutor
                && !user.IsBlocked,
            cancellationToken);

        if (!tutorExists)
        {
            return new CourseCreateResult(false, Error: "The Tutor account is not available.");
        }

        var categoryExists = await dbContext.CourseCategories.AnyAsync(
            category => category.CourseCategoryId == model.CourseCategoryId
                && category.IsActive,
            cancellationToken);

        if (!categoryExists)
        {
            return new CourseCreateResult(
                false,
                Field: nameof(model.CourseCategoryId),
                Error: "Select an active category.");
        }

        var normalizedCode = model.Code.Trim().ToUpperInvariant();
        if (await dbContext.Courses.AnyAsync(
            course => course.Code == normalizedCode,
            cancellationToken))
        {
            return new CourseCreateResult(
                false,
                Field: nameof(model.Code),
                Error: "This course code is already in use.");
        }

        var thumbnailPath = await imageStorage.SaveAsync(
            model.Thumbnail,
            cancellationToken);

        var now = DateTime.UtcNow;
        var course = new Course
        {
            TutorId = tutorId,
            CourseCategoryId = model.CourseCategoryId,
            Code = normalizedCode,
            Title = model.Title.Trim(),
            Slug = await CreateUniqueSlugAsync(
                model.Title,
                normalizedCode,
                cancellationToken),
            ShortDescription = NullIfWhiteSpace(model.ShortDescription),
            Description = model.Description.Trim(),
            ThumbnailPath = thumbnailPath,
            Price = decimal.Round(model.Price, 2, MidpointRounding.AwayFromZero),
            Status = CourseStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.Courses.Add(course);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await imageStorage.DeleteAsync(thumbnailPath);
            return new CourseCreateResult(
                false,
                Error: "The course could not be created. Check the details and try again.");
        }

        return new CourseCreateResult(true, course.CourseId);
    }

    public async Task<CourseActionResult> SubmitForReviewAsync(
        int tutorId,
        int courseId,
        CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses
            .SingleOrDefaultAsync(
                candidate => candidate.CourseId == courseId
                    && candidate.TutorId == tutorId,
                cancellationToken);

        if (course is null)
        {
            return new CourseActionResult(false, "Course not found.");
        }

        if (course.Status is not CourseStatus.Draft and not CourseStatus.Rejected)
        {
            return new CourseActionResult(
                false,
                "Only draft or rejected courses can be submitted for review.");
        }

        var categoryIsActive = await dbContext.CourseCategories.AnyAsync(
            category => category.CourseCategoryId == course.CourseCategoryId
                && category.IsActive,
            cancellationToken);

        if (!categoryIsActive)
        {
            return new CourseActionResult(
                false,
                "Choose an active category before submitting this course.");
        }

        course.Status = CourseStatus.PendingReview;
        course.RejectionReason = null;
        course.ReviewedByUserId = null;
        course.ReviewedAtUtc = null;
        course.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new CourseActionResult(true);
    }

    public async Task<CourseFormViewModel?> GetEditModelAsync(
        int tutorId,
        int courseId,
        CancellationToken cancellationToken)
    {
        var model = await dbContext.Courses
            .AsNoTracking()
            .Where(course => course.CourseId == courseId
                && course.TutorId == tutorId
                && (course.Status == CourseStatus.Draft
                    || course.Status == CourseStatus.Rejected))
            .Select(course => new CourseFormViewModel
            {
                Code = course.Code,
                Title = course.Title,
                CourseCategoryId = course.CourseCategoryId,
                ShortDescription = course.ShortDescription,
                Description = course.Description,
                Price = course.Price,
                ExistingThumbnailPath = course.ThumbnailPath
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (model is not null)
        {
            await PopulateCategoriesAsync(model, cancellationToken);
        }

        return model;
    }

    public async Task<CourseActionResult> UpdateDraftAsync(
        int tutorId,
        int courseId,
        CourseFormViewModel model,
        CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.SingleOrDefaultAsync(
            candidate => candidate.CourseId == courseId
                && candidate.TutorId == tutorId,
            cancellationToken);

        if (course is null)
        {
            return new CourseActionResult(false, "Course not found.");
        }

        if (course.Status is not CourseStatus.Draft and not CourseStatus.Rejected)
        {
            return new CourseActionResult(
                false,
                "Only draft or rejected courses can be edited.");
        }

        var categoryExists = await dbContext.CourseCategories.AnyAsync(
            category => category.CourseCategoryId == model.CourseCategoryId
                && category.IsActive,
            cancellationToken);

        if (!categoryExists)
        {
            return new CourseActionResult(false, "Select an active category.");
        }

        var normalizedCode = model.Code.Trim().ToUpperInvariant();
        if (await dbContext.Courses.AnyAsync(
            candidate => candidate.CourseId != courseId
                && candidate.Code == normalizedCode,
            cancellationToken))
        {
            return new CourseActionResult(false, "This course code is already in use.");
        }

        var replacementThumbnailPath = await imageStorage.SaveAsync(
            model.Thumbnail,
            cancellationToken);
        var previousThumbnailPath = course.ThumbnailPath;

        course.CourseCategoryId = model.CourseCategoryId;
        course.Code = normalizedCode;
        course.Title = model.Title.Trim();
        course.Slug = await CreateUniqueSlugAsync(
            model.Title,
            normalizedCode,
            cancellationToken,
            courseId);
        course.ShortDescription = NullIfWhiteSpace(model.ShortDescription);
        course.Description = model.Description.Trim();
        course.Price = decimal.Round(model.Price, 2, MidpointRounding.AwayFromZero);
        course.ThumbnailPath = replacementThumbnailPath
            ?? (model.RemoveThumbnail ? null : previousThumbnailPath);
        course.Status = CourseStatus.Draft;
        course.RejectionReason = null;
        course.ReviewedByUserId = null;
        course.ReviewedAtUtc = null;
        course.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await imageStorage.DeleteAsync(replacementThumbnailPath);
            return new CourseActionResult(
                false,
                "The course could not be updated. Check the details and try again.");
        }

        if (previousThumbnailPath != course.ThumbnailPath)
        {
            await imageStorage.DeleteAsync(previousThumbnailPath);
        }

        return new CourseActionResult(true);
    }

    public async Task<CourseActionResult> ArchiveAsync(
        int tutorId,
        int courseId,
        CancellationToken cancellationToken)
    {
        var course = await dbContext.Courses.SingleOrDefaultAsync(
            candidate => candidate.CourseId == courseId
                && candidate.TutorId == tutorId,
            cancellationToken);

        if (course is null)
        {
            return new CourseActionResult(false, "Course not found.");
        }

        if (course.Status != CourseStatus.Published)
        {
            return new CourseActionResult(false, "Only a published course can be archived.");
        }

        course.Status = CourseStatus.Archived;
        course.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CourseActionResult(true);
    }

    private async Task<IReadOnlyList<CourseCategoryOptionViewModel>> GetActiveCategoriesAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.CourseCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .Select(category => new CourseCategoryOptionViewModel
            {
                CourseCategoryId = category.CourseCategoryId,
                Name = category.Name
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<string> CreateUniqueSlugAsync(
        string title,
        string code,
        CancellationToken cancellationToken,
        int? excludedCourseId = null)
    {
        var baseSlug = SlugCharactersRegex()
            .Replace(title.Trim().ToLowerInvariant(), "-")
            .Trim('-');

        if (string.IsNullOrEmpty(baseSlug))
        {
            baseSlug = code.ToLowerInvariant();
        }

        baseSlug = baseSlug.Length <= 180 ? baseSlug : baseSlug[..180].TrimEnd('-');
        var slug = baseSlug;
        var suffix = 2;

        while (await dbContext.Courses.AnyAsync(
            course => course.Slug == slug
                && (!excludedCourseId.HasValue
                    || course.CourseId != excludedCourseId.Value),
            cancellationToken))
        {
            var suffixText = $"-{suffix++}";
            var availableLength = 200 - suffixText.Length;
            slug = $"{baseSlug[..Math.Min(baseSlug.Length, availableLength)]}{suffixText}";
        }

        return slug;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex SlugCharactersRegex();
}
