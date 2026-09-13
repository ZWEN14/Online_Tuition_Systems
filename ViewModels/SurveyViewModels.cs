using System.ComponentModel.DataAnnotations;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels;

// Form data and validation used by the controller and Razor view.
public class SurveyEditViewModel
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
    [StringLength(1000)]
    public string? Description { get; set; }
    [Display(Name = "Expiry Date")]
    public DateTime? ExpiresAt { get; set; }
    [Display(Name = "Active")] public bool IsActive { get; set; } = true;
    [Display(Name = "Course (optional)")]
    public int? CourseId { get; set; }
}

public class SurveyCreateViewModel : SurveyEditViewModel
{
    [MinLength(1, ErrorMessage = "Add at least one section.")]
    public List<SurveySectionCreateViewModel> Sections { get; set; } = [new()];
}

public class SurveySectionCreateViewModel
{
    [StringLength(40)]
    public string? DestinationKey { get; set; }
    public int Id { get; set; }
    [Required, StringLength(40)] public string ClientKey { get; set; } = Guid.NewGuid().ToString("N");
    [Required, StringLength(150)] public string Title { get; set; } = "Section 1";
    [StringLength(500)]
    public string? Description { get; set; }
    [MinLength(1, ErrorMessage = "Each section needs at least one question.")]
    public List<SurveyQuestionCreateViewModel> Questions { get; set; } = [new()];
}

public class SurveyQuestionCreateViewModel
{
    public int Id { get; set; }
    public List<SurveyBuilderOptionViewModel> Options { get; set; } = [];
    [Required, StringLength(40)] public string ClientKey { get; set; } = Guid.NewGuid().ToString("N");
    [Required, StringLength(500), Display(Name = "Question")] public string Text { get; set; } = string.Empty;
    [Required, Display(Name = "Question Type")] public SurveyQuestionType Type { get; set; } = SurveyQuestionType.Text;
    [Display(Name = "Required")] public bool IsRequired { get; set; } = true;
}

public class SurveySubmissionViewModel
{
    public int SurveyId { get; set; }
    public string SurveyTitle { get; set; } = string.Empty;
    public string? SurveyDescription { get; set; }
    public List<SurveyAnswerInputViewModel> Answers { get; set; } = [];
    public List<SurveySectionInputViewModel> Sections { get; set; } = [];
}

public class SurveySectionInputViewModel
{
    public SurveyBranchAction AfterSectionAction { get; set; }
    public int? NextSectionId { get; set; }
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public List<int> QuestionIds { get; set; } = [];
}

public class SurveyAnswerInputViewModel
{
    public List<IFormFile> Files { get; set; } = [];
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public SurveyQuestionType Type { get; set; }
    public bool IsRequired { get; set; }
    public string? Value { get; set; }
    public List<string> SelectedValues { get; set; } = [];
    public List<QuestionOptionItemViewModel> Options { get; set; } = [];
}

public class QuestionOptionItemViewModel
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public SurveyBranchAction BranchAction { get; set; }
    public int? DestinationSectionId { get; set; }
}

public class SurveyBuilderOptionViewModel
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Text { get; set; } = string.Empty;
    [StringLength(40)]
    public string? DestinationKey { get; set; }
}
