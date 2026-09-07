using System.ComponentModel.DataAnnotations;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.Events;

public class EventIndexViewModel
{
    [StringLength(100)]
    public string? Search { get; set; }

    public EventMode? Mode { get; set; }

    public RegistrationAudience? Audience { get; set; }

    public EventStatus? Status { get; set; }

    [RegularExpression("upcoming|newest|title")]
    public string Sort { get; set; } = "upcoming";

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 6;

    public int TotalCount { get; set; }

    public IReadOnlyList<Event> Items { get; set; }
        = Array.Empty<Event>();

    public int TotalPages => Math.Max(
        1,
        (int)Math.Ceiling(TotalCount / (double)PageSize));
}
