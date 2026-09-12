using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.Billing;

public sealed class TutorBillingReportViewModel
{
    public int CoursePage { get; init; }
    public int TotalCoursePages { get; init; }
    public int TotalCourses { get; init; }
    public int ActiveEnrollments { get; init; }
    public int PendingPayments { get; init; }
    public int SuccessfulSales { get; init; }
    public decimal GrossSales { get; init; }
    public decimal PlatformFees { get; init; }
    public decimal TutorEarnings { get; init; }
    public IReadOnlyList<CourseStatusMetricViewModel> CourseStatuses { get; init; } = [];
    public IReadOnlyList<TutorCourseRevenueViewModel> LeadingCourses { get; init; } = [];
    public IReadOnlyList<TutorCourseRevenueViewModel> Courses { get; init; } = [];
}

public sealed class TutorCourseRevenueViewModel
{
    public int CourseId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public CourseStatus Status { get; init; }
    public int ActiveEnrollments { get; init; }
    public int PendingPayments { get; init; }
    public int SuccessfulSales { get; init; }
    public decimal GrossSales { get; init; }
    public decimal PlatformFees { get; init; }
    public decimal TutorEarnings { get; init; }
    public int EnrollmentBarPercentage { get; set; }
    public int EarningsBarPercentage { get; set; }
}

public sealed class AdminBillingReportViewModel
{
    public int TotalCourses { get; init; }
    public int TotalEnrollments { get; init; }
    public int PendingPayments { get; init; }
    public int SuccessfulPayments { get; init; }
    public decimal GrossSales { get; init; }
    public decimal PlatformRevenue { get; init; }
    public decimal TutorNetTotal { get; init; }
    public IReadOnlyList<CourseStatusMetricViewModel> CourseStatuses { get; init; } = [];
    public IReadOnlyList<CourseCategoryMetricViewModel> CourseCategories { get; init; } = [];
    public IReadOnlyList<AdminCoursePerformanceViewModel> LeadingCourses { get; init; } = [];
    public IReadOnlyList<AdminPaymentRowViewModel> Payments { get; init; } = [];
}

public sealed class CourseStatusMetricViewModel
{
    public CourseStatus Status { get; init; }
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
    public int Percentage { get; init; }
}

public sealed class CourseCategoryMetricViewModel
{
    public string CategoryName { get; init; } = string.Empty;
    public int CourseCount { get; init; }
    public int Percentage { get; init; }
}

public sealed class AdminCoursePerformanceViewModel
{
    public int CourseId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string TutorName { get; init; } = string.Empty;
    public int ActiveEnrollments { get; init; }
    public int SuccessfulSales { get; init; }
    public decimal GrossSales { get; init; }
    public int EnrollmentBarPercentage { get; set; }
    public int RevenueBarPercentage { get; set; }
}

public sealed class AdminPaymentRowViewModel
{
    public int PaymentId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string TutorName { get; init; } = string.Empty;
    public string CourseCode { get; init; } = string.Empty;
    public string CourseTitle { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public decimal FinalAmount { get; init; }
    public decimal PlatformFee { get; init; }
    public decimal TutorNetAmount { get; init; }
    public PaymentStatus Status { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
