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

    [Required]
    [Display(Name = "Event start")]
    public DateTimeOffset? StartsAt { get; set; }

    [Required]
    [Display(Name = "Event end")]
    public DateTimeOffset? EndsAt { get; set; }

    [Required]
    [Display(Name = "Registration deadline")]
    public DateTimeOffset? ApplicationDeadline { get; set; }

    [Display(Name = "Who may register")]
    public RegistrationAudience RegistrationAudience { get; set; }
        = RegistrationAudience.All;

    [Required]
    [Display(Name = "Event mode")]
    public EventMode? Mode { get; set; }

    [StringLength(255)]
    public string? Location { get; set; }

    [Display(Name = "Meeting platform")]
    public MeetingPlatform? MeetingPlatform { get; set; }

    [StringLength(2048)]
    [Url]
    [Display(Name = "Meeting URL")]
    public string? MeetingUrl { get; set; }

    [Required]
    [Range(1, 1000)]
    [Display(Name = "Maximum participants")]
    public int? MaxParticipants { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var now = DateTimeOffset.Now;

        if (StartsAt.HasValue && StartsAt.Value <= now)
        {
            yield return new ValidationResult(
                "Event start must be in the future.",
                new[] { nameof(StartsAt) });
        }

        if (StartsAt.HasValue
            && EndsAt.HasValue
            && EndsAt.Value <= StartsAt.Value)
        {
            yield return new ValidationResult(
                "Event end must be later than the start.",
                new[] { nameof(EndsAt) });
        }

        if (ApplicationDeadline.HasValue
            && ApplicationDeadline.Value <= now)
        {
            yield return new ValidationResult(
                "Registration deadline must be in the future.",
                new[] { nameof(ApplicationDeadline) });
        }

        if (ApplicationDeadline.HasValue
            && StartsAt.HasValue
            && ApplicationDeadline.Value >= StartsAt.Value)
        {
            yield return new ValidationResult(
                "Registration deadline must be earlier than the event start.",
                new[] { nameof(ApplicationDeadline) });
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
