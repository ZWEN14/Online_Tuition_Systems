using System.ComponentModel.DataAnnotations;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels;

// Form data and validation used by the controller and Razor view.
public class ComplaintCreateViewModel
{
    public List<IFormFile>? Photos { get; set; } = [];
    [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(3000)] public string Description { get; set; } = string.Empty;
    [Required, Display(Name = "Category")]
    public int CategoryId { get; set; }
}

public class ComplaintManageViewModel
{
    public int Id { get; set; }
    [Required, Display(Name = "Category")]
    public int CategoryId { get; set; }
    [Display(Name = "Assigned Tutor")]
    public int? AssignedTutorId { get; set; }
    [Required]
    public ComplaintStatus Status { get; set; }
    [StringLength(3000), Display(Name = "Resolution Notes")]
    public string? ResolutionNotes { get; set; }
}

public class ComplaintIndexViewModel
{
    public List<Complaint> Complaints { get; set; } = [];
    public List<ComplaintCategory> Categories { get; set; } = [];
}
