using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.Services.Billing;

public sealed class PromotionPricingService(ApplicationDbContext dbContext)
    : IPromotionPricingService
{
    public async Task<PromotionPriceResult> CalculateAsync(
        int courseId,
        decimal originalAmount,
        string? promotionCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(promotionCode))
        {
            return new(true, originalAmount, 0m, originalAmount);
        }

        var normalizedCode = promotionCode.Trim().ToUpperInvariant();
        if (normalizedCode.Length > 50
            || normalizedCode.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            return Invalid(originalAmount);
        }

        var now = DateTime.UtcNow;
        var promotion = await dbContext.Promotions
            .AsNoTracking()
            .Where(candidate => candidate.CourseId == courseId
                && candidate.Code == normalizedCode)
            .Select(candidate => new
            {
                candidate.DiscountPercentage,
                candidate.IsActive,
                candidate.StartsAtUtc,
                candidate.EndsAtUtc,
                candidate.RedemptionLimit,
                RedemptionCount = candidate.Course.Enrollments
                    .SelectMany(enrollment => enrollment.Payments)
                    .Count(payment => payment.Status == PaymentStatus.Successful
                        && payment.PromotionCodeSnapshot == candidate.Code)
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (promotion is null
            || !promotion.IsActive
            || promotion.StartsAtUtc > now
            || promotion.EndsAtUtc < now
            || (promotion.RedemptionLimit.HasValue
                && promotion.RedemptionCount >= promotion.RedemptionLimit.Value))
        {
            return Invalid(originalAmount);
        }

        var discountAmount = decimal.Round(
            originalAmount * promotion.DiscountPercentage / 100m,
            2,
            MidpointRounding.AwayFromZero);

        return new(
            true,
            originalAmount,
            discountAmount,
            originalAmount - discountAmount,
            normalizedCode);
    }

    private static PromotionPriceResult Invalid(decimal originalAmount) =>
        new(
            false,
            originalAmount,
            0m,
            originalAmount,
            Error: "This promotion code is invalid, expired, disabled, or no longer available.");
}
