using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

// Groups questions into a page and stores the default destination after that page.
public class SurveySection
{
    public int Id { get; set; }
    public SurveyBranchAction AfterSectionAction { get; set; } = SurveyBranchAction.Continue;
    public int? NextSectionId { get; set; }
    public SurveySection? NextSection { get; set; }
    public int SurveyId { get; set; }
    public Survey? Survey { get; set; }
    [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    [Range(1, 999), Display(Name = "Section Order")] public int DisplayOrder { get; set; } = 1;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
