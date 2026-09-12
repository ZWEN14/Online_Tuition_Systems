using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

public enum SurveyBranchAction
{
    [Display(Name = "Continue to next section")] Continue = 1,
    [Display(Name = "Go to section")] GoToSection = 2,
    [Display(Name = "Submit form")] Submit = 3
}
