using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.Billing;

public sealed class CheckoutViewModel
{
    public int EnrollmentId { get; init; }
    public int CourseId { get; init; }
    public string CourseCode { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string TutorName { get; init; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string? PromotionCodeInput { get; set; }
    public string? AppliedPromotionCode { get; set; }
    public string? PromotionError { get; set; }
    public string Currency { get; init; } = "MYR";
    public bool StripeAvailable { get; set; }
}

public sealed class BillingHistoryViewModel
{
    public IReadOnlyList<BillingHistoryItemViewModel> Payments { get; init; } = [];
}

public sealed class BillingHistoryItemViewModel
{
    public int PaymentId { get; init; }
    public int? InvoiceId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string CourseCode { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public decimal FinalAmount { get; init; }
    public string Currency { get; init; } = "MYR";
    public string Provider { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public PaymentStatus Status { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? PaidAtUtc { get; init; }
}

public sealed class InvoiceDetailsViewModel
{
    public int InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public string PaymentReference { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string CourseCode { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string? PromotionCode { get; init; }
    public decimal OriginalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public string Currency { get; init; } = "MYR";
    public DateTime IssuedAtUtc { get; init; }
}
