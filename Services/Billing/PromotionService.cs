using AnywhereEdureach.Models;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.ViewModels.Billing;

namespace Online_Tuition_Systems.Services.Billing;

public sealed class PromotionService(ApplicationDbContext dbContext) : IPromotionService
{
    public async Task<IReadOnlyList<PromotionListItemViewModel>> GetTutorPromotionsAsync(
        int tutorId,
        CancellationToken cancellationToken)
    {
        if (!await IsAvailableTutorAsync(tutorId, cancellationToken))
        {
            return [];
        }

        var promotions = await dbContext.Promotions
            .AsNoTracking()
            .Where(promotion => promotion.Course.TutorId == tutorId)
            .OrderByDescending(promotion => promotion.CreatedAtUtc)
            .Select(promotion => new
            {
                promotion.PromotionId,
                promotion.Code,
                CourseCode = promotion.Course.Code,
                CourseTitle = promotion.Course.Title,
                OriginalPrice = promotion.Course.Price,
                promotion.DiscountPercentage,
                promotion.StartsAtUtc,
                promotion.EndsAtUtc,
                promotion.IsActive,
                promotion.RedemptionLimit,
                RedemptionCount = promotion.Course.Enrollments
                    .SelectMany(enrollment => enrollment.Payments)
                    .Count(payment => payment.Status == PaymentStatus.Successful
                        && payment.PromotionCodeSnapshot == promotion.Code)
            })
            .ToListAsync(cancellationToken);

        return promotions.Select(promotion =>
        {
            var discountAmount = decimal.Round(
                promotion.OriginalPrice * promotion.DiscountPercentage / 100m,
                2,
                MidpointRounding.AwayFromZero);

            return new PromotionListItemViewModel
            {
                PromotionId = promotion.PromotionId,
                Code = promotion.Code,
                CourseCode = promotion.CourseCode,
                CourseTitle = promotion.CourseTitle,
                OriginalPrice = promotion.OriginalPrice,
                DiscountAmount = discountAmount,
                FinalPrice = promotion.OriginalPrice - discountAmount,
                DiscountPercentage = promotion.DiscountPercentage,
                StartsAtMyt = ToMyt(promotion.StartsAtUtc),
                EndsAtMyt = ToMyt(promotion.EndsAtUtc),
                IsActive = promotion.IsActive,
                RedemptionLimit = promotion.RedemptionLimit,
                RedemptionCount = promotion.RedemptionCount
            };
        }).ToList();
    }

    public async Task PopulateCoursesAsync(
        int tutorId,
        PromotionFormViewModel model,
        CancellationToken cancellationToken)
    {
        model.Courses = await dbContext.Courses
            .AsNoTracking()
            .Where(course => course.TutorId == tutorId
                && course.Status == CourseStatus.Published
                && course.Price > 0)
            .OrderBy(course => course.Title)
            .Select(course => new PromotionCourseOptionViewModel
            {
                CourseId = course.CourseId,
                Code = course.Code,
                Title = course.Title,
                Price = course.Price
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PromotionFormViewModel?> GetEditModelAsync(
        int tutorId,
        int promotionId,
        CancellationToken cancellationToken)
    {
        var model = await dbContext.Promotions
            .AsNoTracking()
            .Where(promotion => promotion.PromotionId == promotionId
                && promotion.Course.TutorId == tutorId)
            .Select(promotion => new PromotionFormViewModel
            {
                PromotionId = promotion.PromotionId,
                CourseId = promotion.CourseId,
                Code = promotion.Code,
                DiscountPercentage = promotion.DiscountPercentage,
                StartsAtMyt = ToMyt(promotion.StartsAtUtc),
                EndsAtMyt = ToMyt(promotion.EndsAtUtc),
                RedemptionLimit = promotion.RedemptionLimit
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (model is not null)
        {
            await PopulateCoursesAsync(tutorId, model, cancellationToken);
        }

        return model;
    }

    public async Task<PromotionActionResult> CreateAsync(
        int tutorId,
        PromotionFormViewModel model,
        CancellationToken cancellationToken)
    {
        var error = await ValidateAsync(tutorId, model, null, cancellationToken);
        if (error is not null)
        {
            return new(false, error);
        }

        dbContext.Promotions.Add(new Promotion
        {
            CourseId = model.CourseId,
            Code = NormalizeCode(model.Code),
            DiscountPercentage = model.DiscountPercentage,
            StartsAtUtc = ToUtc(model.StartsAtMyt),
            EndsAtUtc = ToUtc(model.EndsAtMyt),
            RedemptionLimit = model.RedemptionLimit,
            IsActive = true
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(true);
    }

    public async Task<PromotionActionResult> UpdateAsync(
        int tutorId,
        PromotionFormViewModel model,
        CancellationToken cancellationToken)
    {
        var promotion = await dbContext.Promotions
            .Include(candidate => candidate.Course)
            .SingleOrDefaultAsync(candidate =>
                    candidate.PromotionId == model.PromotionId
                    && candidate.Course.TutorId == tutorId,
                cancellationToken);

        if (promotion is null)
        {
            return new(false, "Promotion not found.");
        }

        var error = await ValidateAsync(
            tutorId,
            model,
            model.PromotionId,
            cancellationToken);
        if (error is not null)
        {
            return new(false, error);
        }

        promotion.CourseId = model.CourseId;
        promotion.Code = NormalizeCode(model.Code);
        promotion.DiscountPercentage = model.DiscountPercentage;
        promotion.StartsAtUtc = ToUtc(model.StartsAtMyt);
        promotion.EndsAtUtc = ToUtc(model.EndsAtMyt);
        promotion.RedemptionLimit = model.RedemptionLimit;
        promotion.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(true);
    }

    public async Task<PromotionActionResult> SetActiveAsync(
        int tutorId,
        int promotionId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var promotion = await dbContext.Promotions
            .SingleOrDefaultAsync(candidate =>
                    candidate.PromotionId == promotionId
                    && candidate.Course.TutorId == tutorId,
                cancellationToken);

        if (promotion is null)
        {
            return new(false, "Promotion not found.");
        }

        promotion.IsActive = isActive;
        promotion.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(true);
    }

    private async Task<string?> ValidateAsync(
        int tutorId,
        PromotionFormViewModel model,
        int? excludedPromotionId,
        CancellationToken cancellationToken)
    {
        if (!await IsAvailableTutorAsync(tutorId, cancellationToken))
        {
            return "Your Tutor account is not available.";
        }

        if (ToUtc(model.EndsAtMyt) <= ToUtc(model.StartsAtMyt))
        {
            return "The end must be later than the start.";
        }

        var ownsEligibleCourse = await dbContext.Courses.AnyAsync(
            course => course.CourseId == model.CourseId
                && course.TutorId == tutorId
                && course.Status == CourseStatus.Published
                && course.Price > 0,
            cancellationToken);
        if (!ownsEligibleCourse)
        {
            return "Select one of your paid published courses.";
        }

        var normalizedCode = NormalizeCode(model.Code);
        var duplicate = await dbContext.Promotions.AnyAsync(
            promotion => promotion.CourseId == model.CourseId
                && promotion.Code == normalizedCode
                && (!excludedPromotionId.HasValue
                    || promotion.PromotionId != excludedPromotionId.Value),
            cancellationToken);

        return duplicate
            ? "That promotion code already exists for this course."
            : null;
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

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static DateTime ToUtc(DateTime mytValue) =>
        DateTime.SpecifyKind(mytValue.AddHours(-8), DateTimeKind.Utc);

    private static DateTime ToMyt(DateTime utcValue) =>
        DateTime.SpecifyKind(
            DateTime.SpecifyKind(utcValue, DateTimeKind.Utc).AddHours(8),
            DateTimeKind.Unspecified);
}
