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

    [Display(Name = "Proposed registration deadline")]
    public DateTimeOffset? ProposedApplicationDeadline { get; set; }

    [Display(Name = "Who may register")]
    public RegistrationAudience ProposedRegistrationAudience { get; set; }
        = RegistrationAudience.All;

    [Display(Name = "Event mode")]
    public EventMode Mode { get; set; } = EventMode.Physical;

    [StringLength(255)]
    public string? Location { get; set; }

    [Display(Name = "Meeting platform")]
    public MeetingPlatform? MeetingPlatform { get; set; }

    [Range(1, 1000)]
    [Display(Name = "Proposed maximum participants")]
    public int ProposedMaxParticipants { get; set; } = 30;

    [Display(Name = "Publish an announcement when approved")]
    public bool PublishAsAnnouncement { get; set; }

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

        if (ProposedApplicationDeadline.HasValue
            && ProposedApplicationDeadline.Value <= now)
        {
            yield return new ValidationResult(
                "The proposed deadline must be in the future.",
                new[] { nameof(ProposedApplicationDeadline) });
        }

        if (ProposedApplicationDeadline.HasValue
            && PreferredStartsAt.HasValue
            && ProposedApplicationDeadline.Value >= PreferredStartsAt.Value)
        {
            yield return new ValidationResult(
                "The proposed deadline must be earlier than the preferred start.",
                new[] { nameof(ProposedApplicationDeadline) });
        }

        if (Mode is EventMode.Physical or EventMode.Hybrid
            && string.IsNullOrWhiteSpace(Location))
        {
            yield return new ValidationResult(
                "Location is required for a physical or hybrid event.",
                new[] { nameof(Location) });
        }

        if (Mode is EventMode.Online or EventMode.Hybrid
            && !MeetingPlatform.HasValue)
        {
            yield return new ValidationResult(
                "Meeting platform is required for an online or hybrid event.",
                new[] { nameof(MeetingPlatform) });
        }
    }
}
