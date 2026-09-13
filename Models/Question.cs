using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

// A survey question belongs to a section; Type determines its input and validation.
public class Question
{
    public int Id { get; set; }
    [Required] public int SurveyId { get; set; }
    public Survey? Survey { get; set; }
    [Required, StringLength(500)] public string Text { get; set; } = string.Empty;
    [Required, Display(Name = "Question Type")] public SurveyQuestionType Type { get; set; }
    [Display(Name = "Required")] public bool IsRequired { get; set; } = true;
    [Range(1, 999)] public int DisplayOrder { get; set; } = 1;
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();

    [Required] public int SectionId { get; set; }
    public SurveySection? Section { get; set; }

}
