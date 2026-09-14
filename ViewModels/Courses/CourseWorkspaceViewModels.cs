using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.Courses;

public static class CourseWorkspaceTabs
{
    public const string Overview = "overview";
    public const string Stream = "stream";
    public const string Lessons = "lessons";
    public const string Coursework = "coursework";
    public const string Students = "students";
    public const string Settings = "settings";

    public static string Normalize(string? value, bool allowStudents = false)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            Stream => Stream,
            Lessons => Lessons,
            Coursework => Coursework,
            Students when allowStudents => Students,
            Settings => Settings,
            _ => Overview
        };
    }
}

public sealed class CourseWorkspaceTabsViewModel
{
    public int CourseId { get; init; }
    public string Controller { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string ActiveTab { get; init; } = CourseWorkspaceTabs.Overview;
    public bool ShowStudents { get; init; }
    public bool ShowSettings { get; init; }
}

public static class CourseStudentSortOptions
{
    public const string Name = "name";
    public const string Newest = "newest";
    public const string Oldest = "oldest";

    public static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            Newest => Newest,
            Oldest => Oldest,
            _ => Name
        };
    }
}

public sealed class CourseStudentItemViewModel
{
    public int StudentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhotoPath { get; init; }
    public DateTime EnrolledAtUtc { get; init; }

    public string Initials
    {
        get
        {
            var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0
                ? "?"
                : string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
        }
    }
}

public sealed class CourseLessonItemViewModel
{
    public int CourseLessonId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string? Content { get; init; }
    public string? ExternalResourceUrl { get; init; }
    public string? ResourceFileName { get; init; }
    public long? ResourceSizeBytes { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsPublished { get; init; }
    public DateTime? AvailableFromUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public bool IsScheduled => IsPublished
        && AvailableFromUtc.HasValue
        && AvailableFromUtc.Value > DateTime.UtcNow;

    public string? ExternalResourceHost
    {
        get
        {
            if (!Uri.TryCreate(ExternalResourceUrl, UriKind.Absolute, out var uri))
            {
                return null;
            }

            return uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                ? uri.Host[4..]
                : uri.Host;
        }
    }

    public string? ExternalResourcePreviewImageUrl
    {
        get
        {
            var videoId = GetYouTubeVideoId();
            return videoId is null
                ? null
                : $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";
        }
    }

    public bool IsYouTubeResource => GetYouTubeVideoId() is not null;

    private string? GetYouTubeVideoId()
    {
        if (!Uri.TryCreate(ExternalResourceUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        string? candidate = null;
        if (uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            candidate = uri.AbsolutePath.Trim('/').Split('/')[0];
        }
        else if (uri.Host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase))
        {
            var segments = uri.AbsolutePath.Trim('/').Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2
                && (segments[0].Equals("embed", StringComparison.OrdinalIgnoreCase)
                    || segments[0].Equals("shorts", StringComparison.OrdinalIgnoreCase)))
            {
                candidate = segments[1];
            }
            else
            {
                candidate = uri.Query.TrimStart('?')
                    .Split('&', StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => part.Split('=', 2, StringSplitOptions.None))
                    .Where(part => part.Length == 2 && part[0] == "v")
                    .Select(part => Uri.UnescapeDataString(part[1]))
                    .FirstOrDefault();
            }
        }

        return candidate?.Length == 11
            && candidate.All(character => char.IsLetterOrDigit(character)
                || character is '-' or '_')
            ? candidate
            : null;
    }
}

public sealed class TutorCourseWorkspaceViewModel
{
    public int CourseId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ThumbnailPath { get; init; }
    public CourseStatus CourseStatus { get; init; }
    public string? RejectionReason { get; init; }
    public string ActiveTab { get; set; } = CourseWorkspaceTabs.Overview;
    public int ActiveEnrollmentCount { get; init; }
    public IReadOnlyList<CourseLessonItemViewModel> Lessons { get; init; } = [];
    public IReadOnlyList<CourseStudentItemViewModel> Students { get; set; } = [];
    public string StudentSort { get; set; } = CourseStudentSortOptions.Name;
    public CourseStreamViewModel? Stream { get; set; }
    public CourseworkWorkspaceViewModel? Coursework { get; set; }
    public bool CanEditSettings => CourseStatus is CourseStatus.Draft or CourseStatus.Rejected;
    public bool CanSubmit => CourseStatus is CourseStatus.Draft or CourseStatus.Rejected;
    public bool CanArchive => CourseStatus == CourseStatus.Published;
}

public enum CourseLessonPublishingMode
{
    Draft,
    PublishNow,
    Schedule
}

public sealed class CourseLessonFormViewModel : IValidatableObject
{
    public int CourseId { get; set; }
    public int? CourseLessonId { get; set; }

    [Required, StringLength(180)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Summary { get; set; }

    [StringLength(20000)]
    [DataType(DataType.MultilineText)]
    public string? Content { get; set; }

    [StringLength(1000)]
    [Url]
    [Display(Name = "External resource or video URL")]
    public string? ExternalResourceUrl { get; set; }

    [Display(Name = "Resource attachment")]
    public IFormFile? ResourceFile { get; set; }

    public string? ExistingResourceFileName { get; set; }

    [Display(Name = "Remove existing attachment")]
    public bool RemoveResourceFile { get; set; }

    [Range(1, 999)]
    [Display(Name = "Lesson order")]
    public int DisplayOrder { get; set; } = 1;

    [Display(Name = "Publishing option")]
    [EnumDataType(typeof(CourseLessonPublishingMode))]
    public CourseLessonPublishingMode PublishingMode { get; set; } =
        CourseLessonPublishingMode.Draft;

    [DataType(DataType.DateTime)]
    [Display(Name = "Available from (MYT)")]
    public DateTime? AvailableFromMyt { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PublishingMode == CourseLessonPublishingMode.Schedule
            && !AvailableFromMyt.HasValue)
        {
            yield return new ValidationResult(
                "Choose the MYT date and time for scheduled publishing.",
                [nameof(AvailableFromMyt)]);
        }
        else if (PublishingMode == CourseLessonPublishingMode.Schedule
            && AvailableFromMyt.HasValue
            && AvailableFromMyt.Value <= DateTime.UtcNow.AddHours(8))
        {
            yield return new ValidationResult(
                "Scheduled publishing must use a future MYT date and time.",
                [nameof(AvailableFromMyt)]);
        }

        if (!string.IsNullOrWhiteSpace(ExternalResourceUrl)
            && (!Uri.TryCreate(ExternalResourceUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            yield return new ValidationResult(
                "Use a complete http:// or https:// resource URL.",
                [nameof(ExternalResourceUrl)]);
        }

        var keepsExistingFile = !string.IsNullOrWhiteSpace(ExistingResourceFileName)
            && !RemoveResourceFile;
        if (string.IsNullOrWhiteSpace(Content)
            && string.IsNullOrWhiteSpace(ExternalResourceUrl)
            && (ResourceFile is null || ResourceFile.Length == 0)
            && !keepsExistingFile)
        {
            yield return new ValidationResult(
                "Add lesson text, an external resource URL, or an attachment.",
                [nameof(Content), nameof(ExternalResourceUrl), nameof(ResourceFile)]);
        }
    }
}
