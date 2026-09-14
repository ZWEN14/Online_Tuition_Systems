using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

// One submission per respondent and survey; contains the saved answers.
public class SurveyResponse
{
    public int Id { get; set; }
    [Required] public int SurveyId { get; set; }
    public Survey? Survey { get; set; }
    [Required] public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
}
