using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AnywhereEdureach.Models;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Stripe;
using Stripe.Checkout;

namespace Online_Tuition_Systems.Services.Billing;

public sealed class StripeCheckoutService(
    ApplicationDbContext dbContext,
    IPromotionPricingService promotionPricingService,
    IConfiguration configuration,
    ILogger<StripeCheckoutService> logger) : IStripeCheckoutService
{
    private const decimal CommissionRate = 15.00m;
    private readonly string secretKey = configuration["Stripe:SecretKey"] ?? string.Empty;

    public bool IsConfigured => secretKey.StartsWith("sk_test_", StringComparison.Ordinal);

    public async Task<StripeCheckoutResult> CreateCheckoutAsync(
        int studentId,
        int enrollmentId,
        string? promotionCode,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return new(false, Message: "Stripe test mode is not configured.");
        }

        var existingInvoiceId = await FindSuccessfulInvoiceIdAsync(
            studentId,
            enrollmentId,
            cancellationToken);

        if (existingInvoiceId.HasValue)
        {
            return new(
                true,
                ExistingInvoiceId: existingInvoiceId,
                Message: "This enrollment has already been paid.");
        }

        var enrollment = await dbContext.Enrollments
            .AsNoTracking()
            .Where(candidate => candidate.EnrollmentId == enrollmentId
                && candidate.StudentId == studentId
                && candidate.Student.Role == UserRole.Student
                && !candidate.Student.IsBlocked
                && candidate.Status == EnrollmentStatus.PendingPayment
                && candidate.Course.Status == CourseStatus.Published
                && candidate.Course.Price > 0)
            .Select(candidate => new
            {
                candidate.EnrollmentId,
                StudentId = candidate.StudentId,
                StudentEmail = candidate.Student.Email,
                CourseId = candidate.CourseId,
                CourseCode = candidate.Course.Code,
                CourseTitle = candidate.Course.Title,
                candidate.Course.Price
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (enrollment is null)
        {
            return new(false, Message: "This enrollment is not available for Stripe Checkout.");
        }

        var pricing = await promotionPricingService.CalculateAsync(
            enrollment.CourseId,
            enrollment.Price,
            promotionCode,
            cancellationToken);
        if (!pricing.Succeeded)
        {
            return new(false, Message: pricing.Error);
        }

        var client = new StripeClient(secretKey);
        var sessionService = new SessionService(client);

        var reusablePayment = await dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.UserId == studentId
                && payment.EnrollmentId == enrollmentId
                && payment.Provider == "Stripe"
                && payment.Status == PaymentStatus.Pending
                && payment.PromotionCodeSnapshot == pricing.PromotionCode
                && payment.FinalAmount == pricing.FinalAmount
                && payment.ProviderCheckoutSessionId != null
                && payment.CheckoutExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(payment => payment.CreatedAtUtc)
            .Select(payment => payment.ProviderCheckoutSessionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(reusablePayment))
        {
            try
            {
                var existingSession = await sessionService.GetAsync(
                    reusablePayment,
                    cancellationToken: cancellationToken);

                if (existingSession.Status == "open"
                    && !string.IsNullOrWhiteSpace(existingSession.Url))
                {
                    return new(true, CheckoutUrl: existingSession.Url);
                }
            }
            catch (StripeException exception)
            {
                logger.LogWarning(
                    "Could not reuse Stripe Checkout Session for enrollment {EnrollmentId}: {StripeMessage}",
                    enrollmentId,
                    exception.StripeError?.Message ?? exception.Message);
            }
        }

        var amountInSen = ToMinorUnits(pricing.FinalAmount);
        var metadata = new Dictionary<string, string>
        {
            ["student_id"] = enrollment.StudentId.ToString(CultureInfo.InvariantCulture),
            ["enrollment_id"] = enrollment.EnrollmentId.ToString(CultureInfo.InvariantCulture)
        };
        if (pricing.PromotionCode is not null)
        {
            metadata["promotion_code"] = pricing.PromotionCode;
        }

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            CustomerEmail = enrollment.StudentEmail,
            ClientReferenceId = enrollment.EnrollmentId.ToString(CultureInfo.InvariantCulture),
            PaymentMethodTypes = ["card"],
            Metadata = metadata,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "myr",
                        UnitAmount = amountInSen,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = enrollment.CourseTitle,
                            Description = pricing.PromotionCode is null
                                ? enrollment.CourseCode
                                : $"{enrollment.CourseCode} - Promotion {pricing.PromotionCode}"
                        }
                    }
                }
            ]
        };

        Stripe.Checkout.Session session;
        try
        {
            session = await sessionService.CreateAsync(
                options,
                cancellationToken: cancellationToken);
        }
        catch (StripeException exception)
        {
            logger.LogWarning(
                "Stripe Checkout creation failed for enrollment {EnrollmentId}: {StripeMessage}",
                enrollmentId,
                exception.StripeError?.Message ?? exception.Message);
            return new(false, Message: "Stripe Checkout could not be started. Please try again.");
        }

        if (string.IsNullOrWhiteSpace(session.Id)
            || string.IsNullOrWhiteSpace(session.Url))
        {
            return new(false, Message: "Stripe did not return a valid Checkout Session.");
        }

        var finalAmount = pricing.FinalAmount;
        var platformFee = CalculatePlatformFee(finalAmount);
        var payment = new Payment
        {
            UserId = studentId,
            EnrollmentId = enrollment.EnrollmentId,
            Reference = BuildReference("STR", session.Id),
            CourseCodeSnapshot = enrollment.CourseCode,
            CourseTitleSnapshot = enrollment.CourseTitle,
            PromotionCodeSnapshot = pricing.PromotionCode,
            OriginalAmount = pricing.OriginalAmount,
            DiscountAmount = pricing.DiscountAmount,
            FinalAmount = finalAmount,
            CommissionRate = CommissionRate,
            PlatformFeeAmount = platformFee,
            TutorNetAmount = finalAmount - platformFee,
            Currency = "MYR",
            Method = "Card",
            Provider = "Stripe",
            ProviderCheckoutSessionId = session.Id,
            Status = PaymentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            CheckoutExpiresAtUtc = session.ExpiresAt.ToUniversalTime()
        };

        dbContext.Payments.Add(payment);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            var existing = await dbContext.Payments
                .AsNoTracking()
                .AnyAsync(candidate =>
                        candidate.ProviderCheckoutSessionId == session.Id
                        && candidate.UserId == studentId,
                    cancellationToken);

            if (!existing)
            {
                return new(false, Message: "The Stripe payment attempt could not be recorded.");
            }
        }

        return new(true, CheckoutUrl: session.Url);
    }

    public async Task<PaymentCompletionResult> CompleteCheckoutAsync(
        int studentId,
        string sessionId,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured
            || string.IsNullOrWhiteSpace(sessionId)
            || sessionId.Length > 255
            || !sessionId.StartsWith("cs_test_", StringComparison.Ordinal))
        {
            return new(false, Message: "The Stripe Checkout Session is invalid.");
        }

        var payment = await dbContext.Payments
            .Include(candidate => candidate.Invoice)
            .Include(candidate => candidate.Enrollment)
                .ThenInclude(enrollment => enrollment.Student)
            .SingleOrDefaultAsync(candidate =>
                    candidate.ProviderCheckoutSessionId == sessionId
                    && candidate.UserId == studentId
                    && candidate.User.Role == UserRole.Student
                    && !candidate.User.IsBlocked
                    && candidate.Provider == "Stripe",
                cancellationToken);

        if (payment is null)
        {
            return new(false, Message: "This Stripe payment attempt does not belong to your account.");
        }

        if (payment.Status == PaymentStatus.Successful && payment.Invoice is not null)
        {
            return new(true, payment.Invoice.InvoiceId, "This Stripe payment was already completed.");
        }

        var client = new StripeClient(secretKey);
        var sessionService = new SessionService(client);
        Stripe.Checkout.Session session;

        try
        {
            session = await sessionService.GetAsync(
                sessionId,
                cancellationToken: cancellationToken);
        }
        catch (StripeException exception)
        {
            logger.LogWarning(
                "Stripe Checkout verification failed for session {SessionId}: {StripeMessage}",
                sessionId,
                exception.StripeError?.Message ?? exception.Message);
            return new(false, Message: "Stripe could not verify this payment. Please try again.");
        }

        var enrollmentIdText = payment.EnrollmentId.ToString(CultureInfo.InvariantCulture);
        var studentIdText = studentId.ToString(CultureInfo.InvariantCulture);
        var hasValidMetadata = session.Metadata is not null
            && session.Metadata.TryGetValue("enrollment_id", out var metadataEnrollmentId)
            && metadataEnrollmentId == enrollmentIdText
            && session.Metadata.TryGetValue("student_id", out var metadataStudentId)
            && metadataStudentId == studentIdText;
        var hasValidPromotionMetadata = payment.PromotionCodeSnapshot is null
            ? session.Metadata is null
                || !session.Metadata.ContainsKey("promotion_code")
            : session.Metadata is not null
                && session.Metadata.TryGetValue("promotion_code", out var metadataPromotionCode)
                && metadataPromotionCode == payment.PromotionCodeSnapshot;

        if (session.Livemode
            || session.Mode != "payment"
            || session.Status != "complete"
            || session.PaymentStatus != "paid"
            || !string.Equals(session.Currency, "myr", StringComparison.OrdinalIgnoreCase)
            || session.AmountTotal != ToMinorUnits(payment.FinalAmount)
            || session.ClientReferenceId != enrollmentIdText
            || !hasValidMetadata
            || !hasValidPromotionMetadata)
        {
            return new(false, Message: "Stripe has not confirmed a matching successful payment.");
        }

        if (payment.Enrollment.Status != EnrollmentStatus.PendingPayment)
        {
            var existingInvoiceId = await FindSuccessfulInvoiceIdAsync(
                studentId,
                payment.EnrollmentId,
                cancellationToken);

            return existingInvoiceId.HasValue
                ? new(true, existingInvoiceId, "This enrollment was already activated.")
                : new(false, Message: "The enrollment is no longer awaiting payment.");
        }

        var now = DateTime.UtcNow;
        payment.Status = PaymentStatus.Successful;
        payment.ProviderPaymentIntentId = session.PaymentIntentId;
        payment.PaidAtUtc = now;
        payment.Enrollment.Status = EnrollmentStatus.Active;
        payment.Enrollment.ActivatedAtUtc = now;
        payment.Invoice = new Online_Tuition_Systems.Models.Invoice
        {
            InvoiceNumber = BuildReference("INV", session.Id),
            CustomerName = payment.Enrollment.Student.Name,
            CustomerEmail = payment.Enrollment.Student.Email,
            CourseCodeSnapshot = payment.CourseCodeSnapshot,
            CourseTitleSnapshot = payment.CourseTitleSnapshot,
            PromotionCodeSnapshot = payment.PromotionCodeSnapshot,
            OriginalAmount = payment.OriginalAmount,
            DiscountAmount = payment.DiscountAmount,
            FinalAmount = payment.FinalAmount,
            CommissionRate = payment.CommissionRate,
            PlatformFeeAmount = payment.PlatformFeeAmount,
            TutorNetAmount = payment.TutorNetAmount,
            Currency = payment.Currency,
            IssuedAtUtc = now
        };

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            var existingInvoiceId = await FindSuccessfulInvoiceIdAsync(
                studentId,
                payment.EnrollmentId,
                cancellationToken);

            return existingInvoiceId.HasValue
                ? new(true, existingInvoiceId, "This Stripe payment was already completed.")
                : new(false, Message: "The verified payment could not be recorded. Please contact an administrator.");
        }

        return new(
            true,
            payment.Invoice.InvoiceId,
            "Stripe payment verified and course access activated.");
    }

    private Task<int?> FindSuccessfulInvoiceIdAsync(
        int studentId,
        int enrollmentId,
        CancellationToken cancellationToken)
    {
        return dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.UserId == studentId
                && payment.EnrollmentId == enrollmentId
                && payment.Status == PaymentStatus.Successful
                && payment.Invoice != null)
            .Select(payment => (int?)payment.Invoice!.InvoiceId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static long ToMinorUnits(decimal amount)
    {
        return checked((long)decimal.Round(
            amount * 100m,
            0,
            MidpointRounding.AwayFromZero));
    }

    private static decimal CalculatePlatformFee(decimal amount)
    {
        return decimal.Round(
            amount * CommissionRate / 100m,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static string BuildReference(string prefix, string sessionId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sessionId));
        return $"{prefix}-{Convert.ToHexString(hash)[..24]}";
    }
}
