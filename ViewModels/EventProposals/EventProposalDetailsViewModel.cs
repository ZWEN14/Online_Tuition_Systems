using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.EventProposals;

public class EventProposalDetailsViewModel
{
    public required EventProposal Proposal { get; set; }

    public ApproveEventProposalViewModel Approval { get; set; } = new();

    public RejectEventProposalViewModel Rejection { get; set; } = new();
}
