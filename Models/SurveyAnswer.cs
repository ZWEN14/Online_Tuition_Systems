using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

public class SurveyAnswer
{
    public ICollection<SubmissionAttachment> Attachments { get; set; } = new List<SubmissionAttachment>();
    public int Id { get; set; }
    [Required] public int SurveyResponseId { get; set; }
    public SurveyResponse? SurveyResponse { get; set; }
    [Required] public int QuestionId { get; set; }
    public Question? Question { get; set; }
    [StringLength(2000)] public string? Value { get; set; }
}
