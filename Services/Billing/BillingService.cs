using AnywhereEdureach.Models;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.ViewModels.Billing;

namespace Online_Tuition_Systems.Services.Billing;

public sealed class BillingService(
    ApplicationDbContext dbContext,
    IPromotionPricingService promotionPricingService) : IBillingService
{
    private const decimal CommissionRate = 15.00m;
    private const int TutorCoursePageSize = 8;

    public async Task<BillingResult<CheckoutViewModel>> GetCheckoutAsync(
        int studentId,
        int enrollmentId,
        string? promotionCode,
        CancellationToken cancellationToken)
    {
        if (!await IsAvailableStudentAsync(studentId, cancellationToken))
        {
            return new(Error: "Your Student account is not available.");
        }

        var checkout = await dbContext.Enrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.EnrollmentId == enrollmentId
                && enrollment.StudentId == studentId
                && enrollment.Status == EnrollmentStatus.PendingPayment
                && enrollment.Course.Status == CourseStatus.Published
                && enrollment.Course.Price > 0)
            .Select(enrollment => new CheckoutViewModel
            {
                EnrollmentId = enrollment.EnrollmentId,
                CourseId = enrollment.CourseId,
                CourseCode = enrollment.Course.Code,
                CourseTitle = enrollment.Course.Title,
                TutorName = enrollment.Course.Tutor.Name,
                OriginalAmount = enrollment.Course.Price,
                FinalAmount = enrollment.Course.Price
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (checkout is null)
        {
            return new(Error: "This enrollment is not available for checkout.");
        }

        var pricing = await promotionPricingService.CalculateAsync(
            checkout.CourseId,
            checkout.OriginalAmount,
            promotionCode,
            cancellationToken);

        checkout.PromotionCodeInput = promotionCode?.Trim().ToUpperInvariant();
        checkout.PromotionError = pricing.Error;
        if (pricing.Succeeded)
        {
            checkout.DiscountAmount = pricing.DiscountAmount;
            checkout.FinalAmount = pricing.FinalAmount;
            checkout.AppliedPromotionCode = pricing.PromotionCode;
        }

        return new(Model: checkout, Error: pricing.Error);
    }

    public async Task<PaymentCompletionResult> CompleteSimulatedPaymentAsync(
        int studentId,
        int enrollmentId,
        string? promotionCode,
        CancellationToken cancellationToken)
    {
        if (!await IsAvailableStudentAsync(studentId, cancellationToken))
        {
            return new(false, Message: "Your Student account is not available.");
        }

        var existingInvoiceId = await FindSuccessfulInvoiceIdAsync(
            studentId,
            enrollmentId,
            cancellationToken);

        if (existingInvoiceId.HasValue)
        {
            return new(
                true,
                existingInvoiceId,
                "This payment was already completed.");
        }

        var enrollment = await dbContext.Enrollments
            .Include(candidate => candidate.Course)
            .Include(candidate => candidate.Student)
            .SingleOrDefaultAsync(candidate =>
                    candidate.EnrollmentId == enrollmentId
                    && candidate.StudentId == studentId,
                cancellationToken);

        if (enrollment is null
            || enrollment.Status != EnrollmentStatus.PendingPayment
            || enrollment.Course.Status != CourseStatus.Published
            || enrollment.Course.Price <= 0)
        {
            return new(false, Message: "This enrollment is not available for payment.");
        }

        var now = DateTime.UtcNow;
        var originalAmount = enrollment.Course.Price;
        var pricing = await promotionPricingService.CalculateAsync(
            enrollment.CourseId,
            originalAmount,
            promotionCode,
            cancellationToken);
        if (!pricing.Succeeded)
        {
            return new(false, Message: pricing.Error);
        }

        var discountAmount = pricing.DiscountAmount;
        var finalAmount = pricing.FinalAmount;
        var platformFee = decimal.Round(
            finalAmount * CommissionRate / 100m,
            2,
            MidpointRounding.AwayFromZero);
        var tutorNet = finalAmount - platformFee;

        var payment = new Payment
        {
            UserId = studentId,
            EnrollmentId = enrollment.EnrollmentId,
            Reference = $"SIM-{enrollment.EnrollmentId:D10}",
            CourseCodeSnapshot = enrollment.Course.Code,
            CourseTitleSnapshot = enrollment.Course.Title,
            PromotionCodeSnapshot = pricing.PromotionCode,
            OriginalAmount = originalAmount,
            DiscountAmount = discountAmount,
            FinalAmount = finalAmount,
            CommissionRate = CommissionRate,
            PlatformFeeAmount = platformFee,
            TutorNetAmount = tutorNet,
            Currency = "MYR",
            Method = "Simulated",
            Provider = "LocalDemo",
            Status = PaymentStatus.Successful,
            CreatedAtUtc = now,
            PaidAtUtc = now
        };

        payment.Invoice = new Invoice
        {
            InvoiceNumber = $"INV-{enrollment.EnrollmentId:D10}",
            CustomerName = enrollment.Student.Name,
            CustomerEmail = enrollment.Student.Email,
            CourseCodeSnapshot = payment.CourseCodeSnapshot,
            CourseTitleSnapshot = payment.CourseTitleSnapshot,
            PromotionCodeSnapshot = pricing.PromotionCode,
            OriginalAmount = originalAmount,
            DiscountAmount = discountAmount,
            FinalAmount = finalAmount,
            CommissionRate = CommissionRate,
            PlatformFeeAmount = platformFee,
            TutorNetAmount = tutorNet,
            Currency = "MYR",
            IssuedAtUtc = now
        };

        enrollment.Status = EnrollmentStatus.Active;
        enrollment.ActivatedAtUtc = now;
        dbContext.Payments.Add(payment);

        try
        {
            // One SaveChanges call makes activation, payment, and invoice atomic.
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            existingInvoiceId = await FindSuccessfulInvoiceIdAsync(
                studentId,
                enrollmentId,
                cancellationToken);

            return existingInvoiceId.HasValue
                ? new(true, existingInvoiceId, "This payment was already completed.")
                : new(false, Message: "Payment could not be completed. Please try again.");
        }

        return new(
            true,
            payment.Invoice.InvoiceId,
            "Payment completed and course access activated.");
    }

    public async Task<BillingHistoryViewModel> GetHistoryAsync(
        int studentId,
        CancellationToken cancellationToken)
    {
        var payments = await dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.UserId == studentId
                && payment.User.Role == UserRole.Student
                && !payment.User.IsBlocked)
            .OrderByDescending(payment => payment.CreatedAtUtc)
            .Select(payment => new BillingHistoryItemViewModel
            {
                PaymentId = payment.PaymentId,
                InvoiceId = payment.Invoice == null
                    ? null
                    : payment.Invoice.InvoiceId,
                Reference = payment.Reference,
                CourseCode = payment.CourseCodeSnapshot,
                CourseTitle = payment.CourseTitleSnapshot,
                FinalAmount = payment.FinalAmount,
                Currency = payment.Currency,
                Provider = payment.Provider,
                Method = payment.Method,
                Status = payment.Status,
                CreatedAtUtc = payment.CreatedAtUtc,
                PaidAtUtc = payment.PaidAtUtc
            })
            .ToListAsync(cancellationToken);

        return new BillingHistoryViewModel { Payments = payments };
    }

    public Task<InvoiceDetailsViewModel?> GetInvoiceAsync(
        int studentId,
        int invoiceId,
        CancellationToken cancellationToken)
    {
        return dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => invoice.InvoiceId == invoiceId
                && invoice.Payment.UserId == studentId
                && invoice.Payment.User.Role == UserRole.Student
                && !invoice.Payment.User.IsBlocked
                && invoice.Payment.Status == PaymentStatus.Successful)
            .Select(invoice => new InvoiceDetailsViewModel
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                PaymentReference = invoice.Payment.Reference,
                CustomerName = invoice.CustomerName,
                CustomerEmail = invoice.CustomerEmail,
                CourseCode = invoice.CourseCodeSnapshot,
                CourseTitle = invoice.CourseTitleSnapshot,
                PromotionCode = invoice.PromotionCodeSnapshot,
                OriginalAmount = invoice.OriginalAmount,
                DiscountAmount = invoice.DiscountAmount,
                FinalAmount = invoice.FinalAmount,
                Currency = invoice.Currency,
                IssuedAtUtc = invoice.IssuedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<TutorBillingReportViewModel?> GetTutorReportAsync(
        int tutorId,
        int coursePage,
        CancellationToken cancellationToken)
    {
        if (!await IsAvailableUserAsync(
                tutorId,
                UserRole.Tutor,
                cancellationToken))
        {
            return null;
        }

        var courses = await dbContext.Courses
            .AsNoTracking()
            .Where(course => course.TutorId == tutorId)
            .OrderBy(course => course.Title)
            .Select(course => new TutorCourseRevenueViewModel
            {
                CourseId = course.CourseId,
                Code = course.Code,
                Title = course.Title,
                Status = course.Status,
                ActiveEnrollments = course.Enrollments.Count(enrollment =>
                    enrollment.Status == EnrollmentStatus.Active),
                PendingPayments = course.Enrollments.Count(enrollment =>
                    enrollment.Status == EnrollmentStatus.PendingPayment),
                SuccessfulSales = course.Enrollments
                    .SelectMany(enrollment => enrollment.Payments)
                    .Count(payment => payment.Status == PaymentStatus.Successful),
                GrossSales = course.Enrollments
                    .SelectMany(enrollment => enrollment.Payments)
                    .Where(payment => payment.Status == PaymentStatus.Successful)
                    .Sum(payment => payment.FinalAmount),
                PlatformFees = course.Enrollments
                    .SelectMany(enrollment => enrollment.Payments)
                    .Where(payment => payment.Status == PaymentStatus.Successful)
                    .Sum(payment => payment.PlatformFeeAmount),
                TutorEarnings = course.Enrollments
                    .SelectMany(enrollment => enrollment.Payments)
                    .Where(payment => payment.Status == PaymentStatus.Successful)
                    .Sum(payment => payment.TutorNetAmount)
            })
            .ToListAsync(cancellationToken);

        var leadingCourses = courses
            .OrderByDescending(course => course.TutorEarnings)
            .ThenByDescending(course => course.ActiveEnrollments)
            .ThenBy(course => course.Title)
            .Take(5)
            .ToList();
        var maximumEnrollments = leadingCourses.Count == 0
            ? 0
            : leadingCourses.Max(course => course.ActiveEnrollments);
        var maximumEarnings = leadingCourses.Count == 0
            ? 0m
            : leadingCourses.Max(course => course.TutorEarnings);

        foreach (var course in leadingCourses)
        {
            course.EnrollmentBarPercentage = ToPercentage(
                course.ActiveEnrollments,
                maximumEnrollments);
            course.EarningsBarPercentage = ToPercentage(
                course.TutorEarnings,
                maximumEarnings);
        }

        var totalCoursePages = Math.Max(
            1,
            (int)Math.Ceiling(courses.Count / (double)TutorCoursePageSize));
        coursePage = Math.Clamp(coursePage, 1, totalCoursePages);

        return new TutorBillingReportViewModel
        {
            CoursePage = coursePage,
            TotalCoursePages = totalCoursePages,
            TotalCourses = courses.Count,
            ActiveEnrollments = courses.Sum(course => course.ActiveEnrollments),
            PendingPayments = courses.Sum(course => course.PendingPayments),
            SuccessfulSales = courses.Sum(course => course.SuccessfulSales),
            GrossSales = courses.Sum(course => course.GrossSales),
            PlatformFees = courses.Sum(course => course.PlatformFees),
            TutorEarnings = courses.Sum(course => course.TutorEarnings),
            CourseStatuses = BuildCourseStatusMetrics(
                courses.Select(course => course.Status)),
            LeadingCourses = leadingCourses,
            Courses = courses
                .Skip((coursePage - 1) * TutorCoursePageSize)
                .Take(TutorCoursePageSize)
                .ToList()
        };
    }

    public async Task<AdminBillingReportViewModel?> GetAdminReportAsync(
        int administratorId,
        CancellationToken cancellationToken)
    {
        if (!await IsAvailableUserAsync(
                administratorId,
                UserRole.Admin,
                cancellationToken))
        {
            return null;
        }

        var pendingPayments = await dbContext.Enrollments
            .AsNoTracking()
            .CountAsync(enrollment =>
                    enrollment.Status == EnrollmentStatus.PendingPayment,
                cancellationToken);

        var courseAnalytics = await dbContext.Courses
            .AsNoTracking()
            .Select(course => new
            {
                course.CourseId,
                course.Code,
                course.Title,
                course.Status,
                CategoryName = course.Category.Name,
                TutorName = course.Tutor.Name,
                TotalEnrollments = course.Enrollments.Count,
                ActiveEnrollments = course.Enrollments.Count(enrollment =>
                    enrollment.Status == EnrollmentStatus.Active),
                SuccessfulSales = course.Enrollments
                    .SelectMany(enrollment => enrollment.Payments)
                    .Count(payment => payment.Status == PaymentStatus.Successful),
                GrossSales = course.Enrollments
                    .SelectMany(enrollment => enrollment.Payments)
                    .Where(payment => payment.Status == PaymentStatus.Successful)
                    .Sum(payment => payment.FinalAmount)
            })
            .ToListAsync(cancellationToken);

        var payments = await dbContext.Payments
            .AsNoTracking()
            .OrderByDescending(payment => payment.CreatedAtUtc)
            .Select(payment => new AdminPaymentRowViewModel
            {
                PaymentId = payment.PaymentId,
                Reference = payment.Reference,
                StudentName = payment.User.Name,
                TutorName = payment.Enrollment.Course.Tutor.Name,
                CourseCode = payment.CourseCodeSnapshot,
                CourseTitle = payment.CourseTitleSnapshot,
                Provider = payment.Provider,
                FinalAmount = payment.FinalAmount,
                PlatformFee = payment.PlatformFeeAmount,
                TutorNetAmount = payment.TutorNetAmount,
                Status = payment.Status,
                CreatedAtUtc = payment.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var successfulPayments = payments
            .Where(payment => payment.Status == PaymentStatus.Successful)
            .ToList();

        var leadingCourses = courseAnalytics
            .OrderByDescending(course => course.GrossSales)
            .ThenByDescending(course => course.ActiveEnrollments)
            .ThenBy(course => course.Title)
            .Take(5)
            .Select(course => new AdminCoursePerformanceViewModel
            {
                CourseId = course.CourseId,
                Code = course.Code,
                Title = course.Title,
                TutorName = course.TutorName,
                ActiveEnrollments = course.ActiveEnrollments,
                SuccessfulSales = course.SuccessfulSales,
                GrossSales = course.GrossSales
            })
            .ToList();
        var maximumCourseEnrollments = leadingCourses.Count == 0
            ? 0
            : leadingCourses.Max(course => course.ActiveEnrollments);
        var maximumCourseRevenue = leadingCourses.Count == 0
            ? 0m
            : leadingCourses.Max(course => course.GrossSales);

        foreach (var course in leadingCourses)
        {
            course.EnrollmentBarPercentage = ToPercentage(
                course.ActiveEnrollments,
                maximumCourseEnrollments);
            course.RevenueBarPercentage = ToPercentage(
                course.GrossSales,
                maximumCourseRevenue);
        }

        var categoryGroups = courseAnalytics
            .GroupBy(course => course.CategoryName)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .ToList();
        var maximumCategoryCourses = categoryGroups.Count == 0
            ? 0
            : categoryGroups.Max(group => group.Count());

        return new AdminBillingReportViewModel
        {
            TotalCourses = courseAnalytics.Count,
            TotalEnrollments = courseAnalytics.Sum(course => course.TotalEnrollments),
            PendingPayments = pendingPayments,
            SuccessfulPayments = successfulPayments.Count,
            GrossSales = successfulPayments.Sum(payment => payment.FinalAmount),
            PlatformRevenue = successfulPayments.Sum(payment => payment.PlatformFee),
            TutorNetTotal = successfulPayments.Sum(payment => payment.TutorNetAmount),
            CourseStatuses = BuildCourseStatusMetrics(
                courseAnalytics.Select(course => course.Status)),
            CourseCategories = categoryGroups
                .Select(group => new CourseCategoryMetricViewModel
                {
                    CategoryName = group.Key,
                    CourseCount = group.Count(),
                    Percentage = ToPercentage(
                        group.Count(),
                        maximumCategoryCourses)
                })
                .ToList(),
            LeadingCourses = leadingCourses,
            Payments = payments
        };
    }

    private static IReadOnlyList<CourseStatusMetricViewModel> BuildCourseStatusMetrics(
        IEnumerable<CourseStatus> statuses)
    {
        var counts = statuses
            .GroupBy(status => status)
            .ToDictionary(group => group.Key, group => group.Count());
        var total = counts.Values.Sum();

        return Enum.GetValues<CourseStatus>()
            .Where(status => counts.ContainsKey(status))
            .Select(status => new CourseStatusMetricViewModel
            {
                Status = status,
                Label = status == CourseStatus.PendingReview
                    ? "Pending review"
                    : status.ToString(),
                Count = counts[status],
                Percentage = ToPercentage(counts[status], total)
            })
            .ToList();
    }

    private static int ToPercentage(decimal value, decimal maximum)
    {
        if (value <= 0m || maximum <= 0m)
        {
            return 0;
        }

        return Math.Clamp(
            (int)decimal.Round(value * 100m / maximum, 0),
            1,
            100);
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
            .SingleOrDefaultAsync(cancellationToken);
    }

    private Task<bool> IsAvailableStudentAsync(
        int studentId,
        CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(
            user => user.Id == studentId
                && user.Role == UserRole.Student
                && !user.IsBlocked,
            cancellationToken);
    }

    private Task<bool> IsAvailableUserAsync(
        int userId,
        UserRole role,
        CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(
            user => user.Id == userId
                && user.Role == role
                && !user.IsBlocked,
            cancellationToken);
    }
}
