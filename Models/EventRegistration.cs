using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Online_Tuition_Systems.Models;

public enum EventRegistrationStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}

[Index(nameof(EventId), nameof(UserId), IsUnique = true)]
public class EventRegistration
{
    public int Id { get; set; }

    public int EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event Event { get; set; } = null!;

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserAccount User { get; set; } = null!;

    [StringLength(1_000)]
    public string? Message { get; set; }

    public EventRegistrationStatus Status { get; set; }
        = EventRegistrationStatus.Pending;

    public int? ReviewedByUserId { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    [StringLength(1_000)]
    public string? ReviewNote { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
