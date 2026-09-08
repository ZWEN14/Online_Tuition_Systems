using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.ViewModels.EventProposals;

public class RequestProposalChangesViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(2_000)]
    [Display(Name = "Changes required")]
    public string ReviewNote { get; set; } = string.Empty;
}
