using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Online_Tuition_Systems.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class CourseImageAttribute : ValidationAttribute
{
    private const long MaximumBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (value is not IFormFile file || file.Length == 0)
        {
            return ValidationResult.Success;
        }

        if (file.Length > MaximumBytes)
        {
            return new ValidationResult("The thumbnail must not exceed 2 MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension)
            || !AllowedContentTypes.Contains(file.ContentType))
        {
            return new ValidationResult("Upload a JPG, PNG, or WebP image.");
        }

        return ValidationResult.Success;
    }
}
