using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.Services;

public interface INotificationService
{
    Task AnnouncementPublishedAsync(Announcement announcement);
    Task EventPublishedAsync(Event tuitionEvent);
    Task EventCancelledAsync(Event tuitionEvent);
    Task ProposalSubmittedAsync(EventProposal proposal);
    Task ProposalUpdatedAsync(EventProposal proposal);
    Task ProposalReviewedAsync(EventProposal proposal);
    Task RegistrationSubmittedAsync(EventRegistration registration);
    Task RegistrationUpdatedAsync(EventRegistration registration);
    Task RegistrationCancelledAsync(EventRegistration registration);
    Task RegistrationReviewedAsync(EventRegistration registration);
}
