using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Online_Tuition_Systems.Models;

public enum UserNotificationType
{
    AnnouncementPublished,
    AnnouncementUpdated,
    EventPublished,
    EventUpdated,
    EventCancelled,
    ProposalSubmitted,
    ProposalUpdated,
    ProposalChangesRequested,
    ProposalApproved,
    ProposalRejected,
    RegistrationSubmitted,
    RegistrationUpdated,
    RegistrationCancelled,
    RegistrationApproved,
    RegistrationRejected
}

[Index(nameof(UserId), nameof(ReadAt), nameof(CreatedAt))]
public class UserNotification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public UserNotificationType Type { get; set; }

    [Required]
    [StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1_000)]
    public string Message { get; set; } = string.Empty;

    [StringLength(2_000)]
    public string? Details { get; set; }

    [StringLength(500)]
    public string? TargetUrl { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsUnread => !ReadAt.HasValue;
}
