using Online_Tuition_Systems.ViewModels.Billing;

namespace Online_Tuition_Systems.Services.Billing;

public interface IPromotionService
{
    Task<IReadOnlyList<PromotionListItemViewModel>> GetTutorPromotionsAsync(
        int tutorId,
        CancellationToken cancellationToken);

    Task PopulateCoursesAsync(
        int tutorId,
        PromotionFormViewModel model,
        CancellationToken cancellationToken);

    Task<PromotionFormViewModel?> GetEditModelAsync(
        int tutorId,
        int promotionId,
        CancellationToken cancellationToken);

    Task<PromotionActionResult> CreateAsync(
        int tutorId,
        PromotionFormViewModel model,
        CancellationToken cancellationToken);

    Task<PromotionActionResult> UpdateAsync(
        int tutorId,
        PromotionFormViewModel model,
        CancellationToken cancellationToken);

    Task<PromotionActionResult> SetActiveAsync(
        int tutorId,
        int promotionId,
        bool isActive,
        CancellationToken cancellationToken);
}

public sealed record PromotionActionResult(bool Succeeded, string? Error = null);
