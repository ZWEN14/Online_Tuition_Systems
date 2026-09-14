using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Online_Tuition_Systems.Models;

[Table("CourseLessons")]
[Index(nameof(CourseId), nameof(DisplayOrder))]
public sealed class CourseLesson
{
    [Key]
    public int CourseLessonId { get; set; }

    [Required]
    public int CourseId { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Summary { get; set; }

    [StringLength(20000)]
    public string? Content { get; set; }

    [StringLength(1000)]
    public string? ExternalResourceUrl { get; set; }

    [StringLength(255)]
    public string? ResourceFileName { get; set; }

    [StringLength(80)]
    public string? ResourceStoredName { get; set; }

    [StringLength(100)]
    public string? ResourceContentType { get; set; }

    public long? ResourceSizeBytes { get; set; }

    [Range(1, 999)]
    public int DisplayOrder { get; set; } = 1;

    public bool IsPublished { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime? AvailableFromUtc { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;
}
