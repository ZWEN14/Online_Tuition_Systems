using System.ComponentModel.DataAnnotations;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.Announcements;

public class AnnouncementIndexViewModel
{
    [StringLength(100)]
    public string? Search { get; set; }

    public AnnouncementAudience? Audience { get; set; }

    public AnnouncementPriority? Priority { get; set; }

    public AnnouncementStatus? Status { get; set; }

    [RegularExpression("newest|oldest|title")]
    public string Sort { get; set; } = "newest";

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 8;

    public int TotalCount { get; set; }

    public IReadOnlyList<Announcement> Items { get; set; }
        = Array.Empty<Announcement>();

    public int TotalPages => Math.Max(
        1,
        (int)Math.Ceiling(TotalCount / (double)PageSize));
}
