namespace Online_Tuition_Systems.Services.Billing;

public interface IPromotionPricingService
{
    Task<PromotionPriceResult> CalculateAsync(
        int courseId,
        decimal originalAmount,
        string? promotionCode,
        CancellationToken cancellationToken);
}

public sealed record PromotionPriceResult(
    bool Succeeded,
    decimal OriginalAmount,
    decimal DiscountAmount,
    decimal FinalAmount,
    string? PromotionCode = null,
    string? Error = null);
