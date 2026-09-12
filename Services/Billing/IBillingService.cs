using Online_Tuition_Systems.ViewModels.Billing;

namespace Online_Tuition_Systems.Services.Billing;

public interface IBillingService
{
    Task<BillingResult<CheckoutViewModel>> GetCheckoutAsync(
        int studentId,
        int enrollmentId,
        string? promotionCode,
        CancellationToken cancellationToken);

    Task<PaymentCompletionResult> CompleteSimulatedPaymentAsync(
        int studentId,
        int enrollmentId,
        string? promotionCode,
        CancellationToken cancellationToken);

    Task<BillingHistoryViewModel> GetHistoryAsync(
        int studentId,
        CancellationToken cancellationToken);

    Task<InvoiceDetailsViewModel?> GetInvoiceAsync(
        int studentId,
        int invoiceId,
        CancellationToken cancellationToken);

    Task<TutorBillingReportViewModel?> GetTutorReportAsync(
        int tutorId,
        int coursePage,
        CancellationToken cancellationToken);

    Task<AdminBillingReportViewModel?> GetAdminReportAsync(
        int administratorId,
        CancellationToken cancellationToken);
}

public sealed record BillingResult<T>(T? Model = default, string? Error = null)
    where T : class;

public sealed record PaymentCompletionResult(
    bool Succeeded,
    int? InvoiceId = null,
    string? Message = null);
