using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Online_Tuition_Systems.Models;

[Table("Invoices")]
[Index(nameof(PaymentId), IsUnique = true)]
[Index(nameof(InvoiceNumber), IsUnique = true)]
public class Invoice
{
    [Key]
    public int InvoiceId { get; set; }

    [Required]
    public int PaymentId { get; set; }

    [Required, StringLength(40)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string CourseCodeSnapshot { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string CourseTitleSnapshot { get; set; } = string.Empty;

    [StringLength(50)]
    public string? PromotionCodeSnapshot { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal OriginalAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal FinalAmount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal CommissionRate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PlatformFeeAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TutorNetAmount { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    [Column(TypeName = "char(3)")]
    public string Currency { get; set; } = "MYR";

    [Column(TypeName = "datetime2")]
    public DateTime IssuedAtUtc { get; set; }

    [ForeignKey(nameof(PaymentId))]
    public Payment Payment { get; set; } = null!;
}
