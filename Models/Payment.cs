using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Online_Tuition_Systems.Models;

[Table("Payments")]
[Index(nameof(Reference), IsUnique = true)]
[Index(nameof(ProviderCheckoutSessionId), IsUnique = true)]
[Index(nameof(Status))]
public class Payment
{
    [Key]
    public int PaymentId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int EnrollmentId { get; set; }

    [Required, StringLength(40)]
    public string Reference { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string CourseCodeSnapshot { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string CourseTitleSnapshot { get; set; } = string.Empty;

    [StringLength(50)]
    public string? PromotionCodeSnapshot { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(typeof(decimal), "0.00", "99999999.99")]
    public decimal OriginalAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(typeof(decimal), "0.00", "99999999.99")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(typeof(decimal), "0.00", "99999999.99")]
    public decimal FinalAmount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    [Range(typeof(decimal), "0.00", "100.00")]
    public decimal CommissionRate { get; set; } = 15.00m;

    [Column(TypeName = "decimal(10,2)")]
    [Range(typeof(decimal), "0.00", "99999999.99")]
    public decimal PlatformFeeAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(typeof(decimal), "0.00", "99999999.99")]
    public decimal TutorNetAmount { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    [Column(TypeName = "char(3)")]
    public string Currency { get; set; } = "MYR";

    [Required, StringLength(30)]
    public string Method { get; set; } = "Simulated";

    [Required, StringLength(30)]
    public string Provider { get; set; } = "Local";

    [StringLength(255)]
    public string? ProviderCheckoutSessionId { get; set; }

    [StringLength(255)]
    public string? ProviderPaymentIntentId { get; set; }

    [Required]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    [StringLength(2000)]
    public string? FailureReason { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime? CheckoutExpiresAtUtc { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime? PaidAtUtc { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(EnrollmentId))]
    public Enrollment Enrollment { get; set; } = null!;

    public Invoice? Invoice { get; set; }
}
