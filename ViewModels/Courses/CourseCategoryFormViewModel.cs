using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.ViewModels.Courses;

public sealed class CourseCategoryFormViewModel
{
    [Required, StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }
}
