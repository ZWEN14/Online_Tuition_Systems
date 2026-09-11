using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

public enum SurveyQuestionType
{
    Text = 1,
    Rating = 2,
    [Display(Name = "Multiple Choice")] MultipleChoice = 3,
    [Display(Name = "Yes / No")] YesNo = 4,
    [Display(Name = "Checkbox / Multiple Select")] Checkbox = 5,
    Dropdown = 6,
    [Display(Name = "Photo / File Upload")] FileUpload = 7
}
