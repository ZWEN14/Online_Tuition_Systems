using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Online_Tuition_Systems.Validation;

namespace Online_Tuition_Systems.ViewModels.Courses;

public sealed class CourseFormViewModel
{
    [Required, StringLength(30)]
    [RegularExpression(
        "^[A-Za-z0-9][A-Za-z0-9-]*$",
        ErrorMessage = "Course code may contain only letters, numbers, and hyphens.")]
    [Display(Name = "Course code")]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Category")]
    public int CourseCategoryId { get; set; }

    [StringLength(500)]
    [Display(Name = "Short description")]
    public string? ShortDescription { get; set; }

    [Required, StringLength(20000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(typeof(decimal), "0.00", "99999999.99")]
    [Display(Name = "Price (MYR)")]
    public decimal Price { get; set; }

    [CourseImage]
    [Display(Name = "Course thumbnail")]
    public IFormFile? Thumbnail { get; set; }

    [Display(Name = "Remove current thumbnail")]
    public bool RemoveThumbnail { get; set; }

    [ValidateNever]
    public string? ExistingThumbnailPath { get; set; }

    [ValidateNever]
    public IReadOnlyList<CourseCategoryOptionViewModel> Categories { get; set; } = [];
}
