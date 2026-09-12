namespace Online_Tuition_Systems.Services.Billing;

public interface IStripeCheckoutService
{
    bool IsConfigured { get; }

    Task<StripeCheckoutResult> CreateCheckoutAsync(
        int studentId,
        int enrollmentId,
        string? promotionCode,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken);

    Task<PaymentCompletionResult> CompleteCheckoutAsync(
        int studentId,
        string sessionId,
        CancellationToken cancellationToken);

    Task<StripeCheckoutResult> CreateBookingCheckoutAsync(
        int studentId,
        int bookingId,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken);

    Task<BookingPaymentCompletionResult> CompleteBookingCheckoutAsync(
        int studentId,
        int bookingId,
        string sessionId,
        CancellationToken cancellationToken);
}

public sealed record StripeCheckoutResult(
    bool Succeeded,
    string? CheckoutUrl = null,
    int? ExistingInvoiceId = null,
    string? Message = null);

public sealed record BookingPaymentCompletionResult(
    bool Succeeded,
    string? Message = null);
