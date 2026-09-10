using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.Courses;

public sealed class AdminCourseReviewViewModel
{
    public int CourseId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string TutorName { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public string? ShortDescription { get; init; }

    public string Description { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public DateTime SubmittedAtUtc { get; init; }
}

public sealed class CourseRejectionViewModel
{
    public int CourseId { get; set; }

    [ValidateNever]
    public string Code { get; set; } = string.Empty;

    [ValidateNever]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(1000, MinimumLength = 5)]
    [Display(Name = "Reason for rejection")]
    public string Reason { get; set; } = string.Empty;
}

public sealed class AdminCourseManagementViewModel
{
    public int CourseId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string TutorName { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public CourseStatus Status { get; init; }

    public string? SuspensionReason { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public bool CanSuspend => Status is CourseStatus.Published or CourseStatus.Archived;

    public bool CanRestore => Status == CourseStatus.Suspended;
}

public sealed class CourseSuspensionViewModel
{
    public int CourseId { get; set; }

    [ValidateNever]
    public string Code { get; set; } = string.Empty;

    [ValidateNever]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(1000, MinimumLength = 5)]
    [Display(Name = "Reason for suspension")]
    public string Reason { get; set; } = string.Empty;
}
