using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

public class SurveyBranchRule
{
    public int Id { get; set; }
    public int QuestionOptionId { get; set; }
    public QuestionOption? QuestionOption { get; set; }
    public SurveyBranchAction Action { get; set; } = SurveyBranchAction.Continue;
    public int? DestinationSectionId { get; set; }
    public SurveySection? DestinationSection { get; set; }
}
