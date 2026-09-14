using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AnywhereEdureach.Models;
using Microsoft.EntityFrameworkCore;

namespace Online_Tuition_Systems.Models;

[Table("CourseAssignments")]
[Index(nameof(CourseId), nameof(IsPublished), nameof(DueAtUtc))]
public sealed class CourseAssignment
{
    [Key]
    public int CourseAssignmentId { get; set; }

    [Required]
    public int CourseId { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(20000)]
    public string Instructions { get; set; } = string.Empty;

    [Column(TypeName = "datetime2")]
    public DateTime DueAtUtc { get; set; }

    public bool IsGraded { get; set; }

    [Column(TypeName = "decimal(7,2)")]
    [Range(typeof(decimal), "1.00", "1000.00")]
    public decimal? MaxMarks { get; set; }

    public bool IsPublished { get; set; }

    [StringLength(255)]
    public string? AttachmentFileName { get; set; }

    [StringLength(255)]
    public string? AttachmentStoredName { get; set; }

    [StringLength(100)]
    public string? AttachmentContentType { get; set; }

    public long? AttachmentSizeBytes { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;

    public ICollection<CourseSubmission> Submissions { get; set; } = [];
}

[Table("CourseSubmissions")]
[Index(nameof(CourseAssignmentId), nameof(StudentId), IsUnique = true)]
public sealed class CourseSubmission
{
    [Key]
    public int CourseSubmissionId { get; set; }

    [Required]
    public int CourseAssignmentId { get; set; }

    [Required]
    public int StudentId { get; set; }

    [StringLength(10000)]
    public string? TextResponse { get; set; }

    [StringLength(255)]
    public string? AttachmentFileName { get; set; }

    [StringLength(255)]
    public string? AttachmentStoredName { get; set; }

    [StringLength(100)]
    public string? AttachmentContentType { get; set; }

    public long? AttachmentSizeBytes { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsLate { get; set; }

    [Column(TypeName = "decimal(7,2)")]
    [Range(typeof(decimal), "0.00", "1000.00")]
    public decimal? Score { get; set; }

    [StringLength(5000)]
    public string? TutorFeedback { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime? GradedAtUtc { get; set; }

    [ForeignKey(nameof(CourseAssignmentId))]
    public CourseAssignment Assignment { get; set; } = null!;

    [ForeignKey(nameof(StudentId))]
    public User Student { get; set; } = null!;
}
