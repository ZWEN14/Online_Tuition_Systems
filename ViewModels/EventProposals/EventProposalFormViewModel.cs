using System.ComponentModel.DataAnnotations;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.EventProposals;

public class EventProposalFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Course ID")]
    public int? CourseId { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(5_000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(2_000)]
    [Display(Name = "Reason for proposing this event")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Preferred start")]
    public DateTimeOffset? PreferredStartsAt { get; set; }

    [Display(Name = "Preferred end")]
    public DateTimeOffset? PreferredEndsAt { get; set; }

    [Display(Name = "Suggested audience")]
    public RegistrationAudience ProposedRegistrationAudience { get; set; }
        = RegistrationAudience.All;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var now = DateTimeOffset.Now;

        if (PreferredStartsAt.HasValue != PreferredEndsAt.HasValue)
        {
            yield return new ValidationResult(
                "Enter both the preferred start and end, or leave both empty.",
                new[] { nameof(PreferredStartsAt), nameof(PreferredEndsAt) });
        }

        if (PreferredStartsAt.HasValue && PreferredStartsAt.Value <= now)
        {
            yield return new ValidationResult(
                "Preferred start must be in the future.",
                new[] { nameof(PreferredStartsAt) });
        }

        if (PreferredStartsAt.HasValue
            && PreferredEndsAt.HasValue
            && PreferredEndsAt.Value <= PreferredStartsAt.Value)
        {
            yield return new ValidationResult(
                "Preferred end must be later than the preferred start.",
                new[] { nameof(PreferredEndsAt) });
        }

    }
}
