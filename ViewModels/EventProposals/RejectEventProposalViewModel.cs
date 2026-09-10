using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.ViewModels.EventProposals;

public class RejectEventProposalViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(2_000)]
    [Display(Name = "Reason for rejection")]
    public string ReviewNote { get; set; } = string.Empty;
}
