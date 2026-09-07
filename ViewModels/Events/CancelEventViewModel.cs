using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.ViewModels.Events;

public class CancelEventViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(2_000, MinimumLength = 5)]
    [Display(Name = "Cancellation reason")]
    public string Reason { get; set; } = string.Empty;
}
