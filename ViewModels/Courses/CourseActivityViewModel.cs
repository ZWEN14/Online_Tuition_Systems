using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.ViewModels.Courses;

public sealed class CourseActivityViewModel
{
    public IReadOnlyList<Event> UpcomingEvents { get; init; } = [];

    public IReadOnlyList<Announcement> Announcements { get; init; } = [];
}
