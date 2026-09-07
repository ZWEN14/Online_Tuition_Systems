using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.ViewModels.EventRegistrations;

public class RejectEventRegistrationViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "A rejection reason is required.")]
    [StringLength(1_000, ErrorMessage = "The rejection reason cannot exceed 1,000 characters.")]
    public string ReviewNote { get; set; } = string.Empty;
}
