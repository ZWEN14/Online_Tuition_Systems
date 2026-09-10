using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.ViewModels.EventProposals;

public class ApproveEventProposalViewModel
{
    public int Id { get; set; }

    [StringLength(2_000)]
    [Display(Name = "Approval note (optional)")]
    public string? ReviewNote { get; set; }
}
