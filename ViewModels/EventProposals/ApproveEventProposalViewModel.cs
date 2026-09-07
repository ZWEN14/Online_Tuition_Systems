using System.ComponentModel.DataAnnotations;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.EventProposals;

public class ApproveEventProposalViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Final start")]
    public DateTimeOffset StartsAt { get; set; }

    [Required]
    [Display(Name = "Final end")]
    public DateTimeOffset EndsAt { get; set; }

    [Display(Name = "Registration deadline")]
    public DateTimeOffset? ApplicationDeadline { get; set; }

    [Display(Name = "Who may register")]
    public RegistrationAudience RegistrationAudience { get; set; }
        = RegistrationAudience.All;

    [Display(Name = "Event mode")]
    public EventMode Mode { get; set; } = EventMode.Physical;

    [StringLength(255)]
    public string? Location { get; set; }

    [Display(Name = "Meeting platform")]
    public MeetingPlatform? MeetingPlatform { get; set; }

    [StringLength(2048)]
    [Url]
    [Display(Name = "Meeting URL")]
    public string? MeetingUrl { get; set; }

    [Range(1, 1000)]
    [Display(Name = "Maximum participants")]
    public int MaxParticipants { get; set; } = 30;

    [Display(Name = "Publish an announcement for this event")]
    public bool PublishAsAnnouncement { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartsAt <= DateTimeOffset.Now)
        {
            yield return new ValidationResult(
                "Final start must be in the future.",
                new[] { nameof(StartsAt) });
        }

        if (EndsAt <= StartsAt)
        {
            yield return new ValidationResult(
                "Final end must be later than the final start.",
                new[] { nameof(EndsAt) });
        }

        if (ApplicationDeadline.HasValue
            && ApplicationDeadline.Value >= StartsAt)
        {
            yield return new ValidationResult(
                "Registration deadline must be earlier than the final start.",
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

        if (Mode is EventMode.Online or EventMode.Hybrid
            && string.IsNullOrWhiteSpace(MeetingUrl))
        {
            yield return new ValidationResult(
                "Meeting URL is required for an online or hybrid event.",
                new[] { nameof(MeetingUrl) });
        }
    }
}
