using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

public enum EventStatus
{
    Draft,
    Published,
    Cancelled,
    Archived
}

public enum EventMode
{
    Physical,
    Online,
    Hybrid
}

public enum MeetingPlatform
{
    Zoom,
    GoogleMeet,
    MicrosoftTeams
}

public enum RegistrationAudience
{
    All,
    Student,
    Tutor
}

public class Event
{
    public int Id { get; set; }

    public int? CourseId { get; set; }

    [StringLength(450)]
    public string? CreatedByUserId { get; set; }

    [StringLength(450)]
    public string? OrganizerUserId { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(10_000)]
    public string Description { get; set; } = string.Empty;

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    public DateTimeOffset? ApplicationDeadline { get; set; }

    public RegistrationAudience RegistrationAudience { get; set; }
        = RegistrationAudience.All;

    public EventMode Mode { get; set; } = EventMode.Physical;

    [StringLength(255)]
    public string? Location { get; set; }

    public MeetingPlatform? MeetingPlatform { get; set; }

    [StringLength(2048)]
    [Url]
    public string? MeetingUrl { get; set; }

    [Range(1, 1000)]
    public int MaxParticipants { get; set; } = 1;

    public EventStatus Status { get; set; } = EventStatus.Draft;

    [StringLength(450)]
    public string? CancelledByUserId { get; set; }

    [StringLength(2_000)]
    public string? CancellationReason { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Announcement? Announcement { get; set; }

    public EventProposal? SourceProposal { get; set; }

    public ICollection<EventRegistration> Registrations { get; set; }
        = new List<EventRegistration>();

    public bool HasOnlineDelivery()
    {
        return Mode is EventMode.Online or EventMode.Hybrid;
    }
}
