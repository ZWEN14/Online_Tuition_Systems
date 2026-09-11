using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

public class Survey
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [Display(Name = "Expiry Date")] public DateTime? ExpiresAt { get; set; }
    [Display(Name = "Active")] public bool IsActive { get; set; } = true;
    [Display(Name = "Course")] public int? CourseId { get; set; }
    public Course? Course { get; set; }
    [Required] public int CreatorId { get; set; }
    public User? Creator { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<SurveySection> Sections { get; set; } = new List<SurveySection>();
    public ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();
}
