using System.ComponentModel.DataAnnotations;
using Online_Tuition_Systems.Authorization;

namespace Online_Tuition_Systems.Models;

public class UserAccount
{
    public int Id { get; set; }

    [Required]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(1_000)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Role { get; set; } = AppRoles.Student;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<EventRegistration> EventRegistrations { get; set; }
        = new List<EventRegistration>();

    public ICollection<UserNotification> Notifications { get; set; }
        = new List<UserNotification>();
}
