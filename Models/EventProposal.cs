using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

public enum EventProposalStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}

public class EventProposal
{
    public int Id { get; set; }

    public int? CourseId { get; set; }

    [Required]
    [StringLength(450)]
    public string ProposedByUserId { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(10_000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(2_000)]
    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset? PreferredStartsAt { get; set; }

    public DateTimeOffset? PreferredEndsAt { get; set; }

    public DateTimeOffset? ProposedApplicationDeadline { get; set; }

    public RegistrationAudience ProposedRegistrationAudience { get; set; }
        = RegistrationAudience.All;

    public EventMode Mode { get; set; } = EventMode.Physical;

    [StringLength(255)]
    public string? Location { get; set; }

    public MeetingPlatform? MeetingPlatform { get; set; }

    [Range(1, 1000)]
    public int ProposedMaxParticipants { get; set; } = 1;

    public bool PublishAsAnnouncement { get; set; }

    public EventProposalStatus Status { get; set; } = EventProposalStatus.Pending;

    [StringLength(450)]
    public string? ReviewedByUserId { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    [StringLength(2_000)]
    public string? ReviewNote { get; set; }

    public DateTimeOffset? LastRevisedAt { get; set; }

    public int RevisionCount { get; set; }

    public int? CreatedEventId { get; set; }

    public Event? CreatedEvent { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
