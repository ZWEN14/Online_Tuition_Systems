using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Online_Tuition_Systems.ViewModels.Courses;

public sealed class CourseworkWorkspaceViewModel
{
    public int CourseId { get; init; }
    public bool IsTutor { get; init; }
    public bool CanManage { get; init; }
    public bool CanSubmit { get; init; }
    public IReadOnlyList<CourseAssignmentItemViewModel> Assignments { get; init; } = [];
}

public sealed class CourseAssignmentItemViewModel
{
    public int CourseAssignmentId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Instructions { get; init; } = string.Empty;
    public DateTime DueAtUtc { get; init; }
    public bool IsGraded { get; init; }
    public decimal? MaxMarks { get; init; }
    public bool IsPublished { get; init; }
    public string? AttachmentFileName { get; init; }
    public int SubmissionCount { get; init; }
    public int GradedCount { get; init; }
    public CourseSubmissionSummaryViewModel? MySubmission { get; init; }
    public bool IsPastDue => DueAtUtc <= DateTime.UtcNow;
}

public sealed class CourseSubmissionSummaryViewModel
{
    public int CourseSubmissionId { get; init; }
    public DateTime SubmittedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public decimal? Score { get; init; }
    public string? TutorFeedback { get; init; }
    public string? AttachmentFileName { get; init; }
    public bool IsLate { get; init; }
    public bool IsGraded => Score.HasValue || !string.IsNullOrWhiteSpace(TutorFeedback);
}

public sealed class CourseAssignmentFormViewModel : IValidatableObject
{
    public int CourseId { get; set; }
    public int? CourseAssignmentId { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(20000)]
    [DataType(DataType.MultilineText)]
    public string Instructions { get; set; } = string.Empty;

    [Required, DataType(DataType.DateTime)]
    [Display(Name = "Date and time (MYT)")]
    public DateTime DueAtMyt { get; set; } = DateTime.UtcNow.AddHours(8).AddDays(7);

    [Display(Name = "Grading option")]
    public bool IsGraded { get; set; } = true;

    [Range(typeof(decimal), "1.00", "1000.00")]
    [Display(Name = "Marks")]
    public decimal? MaxMarks { get; set; } = 100m;

    [Display(Name = "Publishing option")]
    public AssignmentPublishingMode PublishingMode { get; set; } = AssignmentPublishingMode.Draft;

    public bool IsPublished { get; set; }

    [Display(Name = "Assignment attachment")]
    public IFormFile? Attachment { get; set; }

    public string? ExistingAttachmentFileName { get; set; }

    [Display(Name = "Remove existing attachment")]
    public bool RemoveAttachment { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DueAtMyt <= DateTime.UtcNow.AddHours(8))
        {
            yield return new ValidationResult(
                "The Coursework deadline must be in the future.",
                [nameof(DueAtMyt)]);
        }

        if (IsGraded && !MaxMarks.HasValue)
        {
            yield return new ValidationResult(
                "Enter the marks for this graded assignment.",
                [nameof(MaxMarks)]);
        }
    }
}

public enum AssignmentPublishingMode
{
    Draft,
    Publish
}

public sealed class StudentAssignmentViewModel
{
    public int CourseId { get; init; }
    public int CourseAssignmentId { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Instructions { get; init; } = string.Empty;
    public DateTime DueAtUtc { get; init; }
    public bool IsGraded { get; init; }
    public decimal? MaxMarks { get; init; }
    public string? AssignmentAttachmentFileName { get; init; }
    public CourseSubmissionFormViewModel Submission { get; init; } = new();
    public bool IsPastDue => DueAtUtc <= DateTime.UtcNow;
    public bool CanSubmit { get; init; }
}

public sealed class CourseSubmissionFormViewModel : IValidatableObject
{
    public int? CourseSubmissionId { get; set; }
    public int CourseId { get; set; }
    public int CourseAssignmentId { get; set; }

    [StringLength(10000)]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Response")]
    public string? TextResponse { get; set; }

    [Display(Name = "Submission attachment")]
    public IFormFile? Attachment { get; set; }

    public string? ExistingAttachmentFileName { get; set; }

    [Display(Name = "Remove saved attachment")]
    public bool RemoveAttachment { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsLate { get; set; }
    public decimal? Score { get; set; }
    public string? TutorFeedback { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(TextResponse)
            && (Attachment is null || Attachment.Length == 0)
            && (string.IsNullOrWhiteSpace(ExistingAttachmentFileName) || RemoveAttachment))
        {
            yield return new ValidationResult(
                "Add a written response or attach a file.",
                [nameof(TextResponse), nameof(Attachment)]);
        }
    }
}

public sealed class CourseworkSubmissionsViewModel
{
    public int CourseId { get; init; }
    public int CourseAssignmentId { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public string AssignmentTitle { get; init; } = string.Empty;
    public bool IsGraded { get; init; }
    public decimal? MaxMarks { get; init; }
    public int ActiveEnrollmentCount { get; init; }
    public IReadOnlyList<CourseworkSubmissionItemViewModel> Submissions { get; init; } = [];
}

public sealed class CourseworkSubmissionItemViewModel
{
    public int CourseSubmissionId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public DateTime SubmittedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public bool IsLate { get; init; }
    public string? TextResponse { get; init; }
    public string? AttachmentFileName { get; init; }
    public decimal? Score { get; init; }
    public string? TutorFeedback { get; init; }
}

public sealed class CourseworkGradeViewModel : IValidatableObject
{
    public int CourseId { get; set; }
    public int CourseAssignmentId { get; set; }
    public int CourseSubmissionId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public bool IsGraded { get; set; }
    public decimal? MaxMarks { get; set; }

    [Range(typeof(decimal), "0.00", "1000.00")]
    public decimal? Score { get; set; }

    [StringLength(5000)]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Private feedback")]
    public string? TutorFeedback { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Score.HasValue && string.IsNullOrWhiteSpace(TutorFeedback))
        {
            yield return new ValidationResult(
                IsGraded ? "Enter a mark or private feedback." : "Enter private feedback.",
                [nameof(Score), nameof(TutorFeedback)]);
        }

        if (IsGraded && Score > MaxMarks)
        {
            yield return new ValidationResult(
                $"The mark cannot exceed {MaxMarks:0.##}.",
                [nameof(Score)]);
        }
    }
}
