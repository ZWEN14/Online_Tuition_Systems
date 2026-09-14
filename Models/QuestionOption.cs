using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

// One selectable answer for a choice question.
public class QuestionOption
{
    public int Id { get; set; }
    [Required] public int QuestionId { get; set; }
    public Question? Question { get; set; }
    [Required, StringLength(200)] public string Text { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 1;
    public SurveyBranchRule? BranchRule { get; set; }
}
