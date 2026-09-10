using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using AnywhereEdureach.Models;

namespace Online_Tuition_Systems.Models;

[Table("Courses")]
[Index(nameof(Code), IsUnique = true)]
[Index(nameof(Slug), IsUnique = true)]
[Index(nameof(Status), nameof(PublishedAtUtc))]
public class Course
{
    [Key]
    public int CourseId { get; set; }

    [Required]
    public int TutorId { get; set; }

    [Required]
    public int CourseCategoryId { get; set; }

    public int? ReviewedByUserId { get; set; }

    [Required, StringLength(30)]
    [RegularExpression("^[A-Za-z0-9][A-Za-z0-9-]*$",
        ErrorMessage = "Course code may contain only letters, numbers, and hyphens.")]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ShortDescription { get; set; }

    [Required, StringLength(20000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ThumbnailPath { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(typeof(decimal), "0.00", "99999999.99")]
    public decimal Price { get; set; }

    [Required]
    public CourseStatus Status { get; set; } = CourseStatus.Draft;

    public CourseStatus? StatusBeforeSuspension { get; set; }

    [StringLength(1000)]
    public string? RejectionReason { get; set; }

    [StringLength(1000)]
    public string? SuspensionReason { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime? ReviewedAtUtc { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime? PublishedAtUtc { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TutorId))]
    public User Tutor { get; set; } = null!;

    [ForeignKey(nameof(CourseCategoryId))]
    public CourseCategory Category { get; set; } = null!;

    [ForeignKey(nameof(ReviewedByUserId))]
    public User? ReviewedBy { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = [];
}
