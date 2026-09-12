namespace Online_Tuition_Systems.Models;

public enum CourseStatus
{
    Draft = 1,
    PendingReview = 2,
    Rejected = 3,
    Published = 4,
    Archived = 5,
    Suspended = 6
}

public enum EnrollmentStatus
{
    PendingPayment = 1,
    Active = 2,
    Cancelled = 3
}

public enum PaymentStatus
{
    Pending = 1,
    Successful = 2,
    Failed = 3,
    Cancelled = 4,
    Expired = 5
}
