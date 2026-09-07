using System.ComponentModel.DataAnnotations;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.EventRegistrations;

public class RegisterEventViewModel
{
    public int EventId { get; set; }

    [StringLength(1_000, ErrorMessage = "Message cannot exceed 1,000 characters.")]
    [Display(Name = "Message to the organiser (optional)")]
    public string? Message { get; set; }

    public Event? Event { get; set; }
}
