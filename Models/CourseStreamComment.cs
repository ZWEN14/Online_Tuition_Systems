using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AnywhereEdureach.Models;
using Microsoft.EntityFrameworkCore;

namespace Online_Tuition_Systems.Models;

[Table("CourseStreamComments")]
[Index(nameof(AnnouncementId), nameof(CreatedAtUtc))]
public sealed class CourseStreamComment
{
    [Key]
    public int CourseStreamCommentId { get; set; }

    [Required]
    public int AnnouncementId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required, StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    [Column(TypeName = "datetime2")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsRemovedByTutor { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime? RemovedAtUtc { get; set; }

    [StringLength(100)]
    public string? RemovedByTutorName { get; set; }

    [ForeignKey(nameof(AnnouncementId))]
    public Announcement Announcement { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
