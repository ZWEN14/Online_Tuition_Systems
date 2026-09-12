using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

public class SubmissionAttachment
{
    public int Id { get; set; }
    [MaxLength(255)] public string FileName { get; set; } = string.Empty;
    [MaxLength(100)] public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
    public int? SurveyAnswerId { get; set; }
    public SurveyAnswer? SurveyAnswer { get; set; }
    public int? ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }
}
