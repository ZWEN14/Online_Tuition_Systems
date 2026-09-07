using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.Events;

public class EventDetailsViewModel
{
    public Event Event { get; set; } = null!;

    public EventRegistration? CurrentRegistration { get; set; }

    public int ApprovedRegistrationCount { get; set; }

    public int TotalRegistrationCount { get; set; }

    public bool CanRegister { get; set; }

    public bool CanViewMeetingUrl { get; set; }

    public string? RegistrationUnavailableReason { get; set; }
}
