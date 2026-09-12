using System.ComponentModel.DataAnnotations;
namespace Online_Tuition_Systems.ViewModels.Billing;

public sealed class PromotionFormViewModel : IValidatableObject
{
    public int PromotionId { get; set; }

    [Required(ErrorMessage = "Select a paid published course.")]
    [Display(Name = "Course")]
    public int CourseId { get; set; }

    [Required, StringLength(50)]
    [RegularExpression("^[A-Za-z0-9-]+$",
        ErrorMessage = "Use only letters, numbers, and hyphens.")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Range(typeof(decimal), "5.00", "90.00")]
    [Display(Name = "Discount")]
    public decimal DiscountPercentage { get; set; } = 10m;

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Starts at (MYT)")]
    public DateTime StartsAtMyt { get; set; } = DateTime.UtcNow.AddHours(8).AddMinutes(-5);

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Ends at (MYT)")]
    public DateTime EndsAtMyt { get; set; } = DateTime.UtcNow.AddHours(8).AddDays(30);

    [Range(1, 100000)]
    [Display(Name = "Redemption limit")]
    public int? RedemptionLimit { get; set; }

    public IReadOnlyList<PromotionCourseOptionViewModel> Courses { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndsAtMyt <= StartsAtMyt)
        {
            yield return new ValidationResult(
                "The end must be later than the start.",
                [nameof(EndsAtMyt)]);
        }
    }
}

public sealed class PromotionCourseOptionViewModel
{
    public int CourseId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public decimal Price { get; init; }
}

public sealed class PromotionListItemViewModel
{
    public int PromotionId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string CourseCode { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public decimal OriginalPrice { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalPrice { get; init; }
    public decimal DiscountPercentage { get; init; }
    public DateTime StartsAtMyt { get; init; }
    public DateTime EndsAtMyt { get; init; }
    public bool IsActive { get; init; }
    public int? RedemptionLimit { get; init; }
    public int RedemptionCount { get; init; }
}
