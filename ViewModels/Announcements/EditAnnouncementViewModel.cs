using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.Announcements;

public class EditAnnouncementViewModel
{
    public int Id { get; set; }

    [Display(Name = "Related course")]
    [Range(1, int.MaxValue, ErrorMessage = "Select a valid course.")]
    public int? CourseId { get; set; }

    public List<SelectListItem> CourseOptions { get; set; } = [];

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    [StringLength(10_000, ErrorMessage = "Content cannot exceed 10,000 characters.")]
    public string Content { get; set; } = string.Empty;

    [EnumDataType(typeof(AnnouncementAudience))]
    public AnnouncementAudience Audience { get; set; }

    [EnumDataType(typeof(AnnouncementPriority))]
    public AnnouncementPriority Priority { get; set; }

    [Display(Name = "Expiry date and time")]
    [DataType(DataType.DateTime)]
    public DateTimeOffset? ExpiresAt { get; set; }
}
