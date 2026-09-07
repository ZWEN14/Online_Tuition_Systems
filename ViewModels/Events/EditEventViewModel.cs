using System.ComponentModel.DataAnnotations;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.Events;

public class EditEventViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Course ID")]
    public int? CourseId { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(10_000)]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Starts at")]
    public DateTimeOffset StartsAt { get; set; }

    [Display(Name = "Ends at")]
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
    public int MaxParticipants { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartsAt <= DateTimeOffset.Now)
        {
            yield return new ValidationResult(
                "Start time must be in the future.",
                new[] { nameof(StartsAt) });
        }

        if (EndsAt <= StartsAt)
        {
            yield return new ValidationResult(
                "End time must be later than the start time.",
                new[] { nameof(EndsAt) });
        }

        if (ApplicationDeadline.HasValue
            && ApplicationDeadline.Value >= StartsAt)
        {
            yield return new ValidationResult(
                "Registration deadline must be earlier than the start time.",
                new[] { nameof(ApplicationDeadline) });
        }

        if (Mode is EventMode.Physical or EventMode.Hybrid
            && string.IsNullOrWhiteSpace(Location))
        {
            yield return new ValidationResult(
                "Location is required for a physical or hybrid event.",
                new[] { nameof(Location) });
        }

        if (Mode is EventMode.Online or EventMode.Hybrid)
        {
            if (!MeetingPlatform.HasValue)
            {
                yield return new ValidationResult(
                    "Meeting platform is required for an online or hybrid event.",
                    new[] { nameof(MeetingPlatform) });
            }

            if (string.IsNullOrWhiteSpace(MeetingUrl))
            {
                yield return new ValidationResult(
                    "Meeting URL is required for an online or hybrid event.",
                    new[] { nameof(MeetingUrl) });
            }
        }
    }
}
