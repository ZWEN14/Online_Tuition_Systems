using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Online_Tuition_Systems.Models;

[Table("Promotions")]
[Index(nameof(CourseId), nameof(Code), IsUnique = true)]
[Index(nameof(IsActive), nameof(StartsAtUtc), nameof(EndsAtUtc))]
public sealed class Promotion
{
    [Key]
    public int PromotionId { get; set; }

    [Required]
    public int CourseId { get; set; }

    [Required, StringLength(50)]
    [RegularExpression("^[A-Z0-9-]+$",
        ErrorMessage = "Use only letters, numbers, and hyphens.")]
    public string Code { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5,2)")]
    [Range(typeof(decimal), "5.00", "90.00")]
    public decimal DiscountPercentage { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime StartsAtUtc { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime EndsAtUtc { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(1, 100000)]
    public int? RedemptionLimit { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;
}
