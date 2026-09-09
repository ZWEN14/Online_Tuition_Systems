namespace AnywhereEdureach;

public class NotificationService(ApplicationDbContext db)
{
    public void NotifyBookingCreated(Booking booking)
    {
        var tutor = db.Users.Find(booking.TutorId);
        var student = db.Users.Find(booking.StudentId);

        db.BookingNotifications.Add(new Notification
        {
            UserId = booking.StudentId,
            BookingId = booking.Id,
            Message = $"Your booking request with {tutor?.Name} has been sent."
        });

        db.BookingNotifications.Add(new Notification
        {
            UserId = booking.TutorId,
            BookingId = booking.Id,
            Message = $"You have a new booking request from {student?.Name}."
        });

        db.SaveChanges();
    }

    public void NotifyBookingStatusChanged(Booking booking)
    {
        var tutor = db.Users.Find(booking.TutorId);
        var studentMessage = booking.Status switch
        {
            BookingStatus.Accepted => $"Your booking request has been accepted by {tutor?.Name}.",
            BookingStatus.Rejected => $"Your booking request has been rejected by {tutor?.Name}.",
            BookingStatus.Cancelled => "Your booking has been cancelled.",
            BookingStatus.Completed => "Your booking has been marked as completed.",
            BookingStatus.Paid => "Your booking has been marked as paid.",
            _ => "Your booking status has been updated."
        };

        db.BookingNotifications.Add(new Notification
        {
            UserId = booking.StudentId,
            BookingId = booking.Id,
            Message = studentMessage
        });

        db.BookingNotifications.Add(new Notification
        {
            UserId = booking.TutorId,
            BookingId = booking.Id,
            Message = $"The booking status has been updated to {booking.Status.ToString().ToLowerInvariant()}."
        });

        db.SaveChanges();
    }
}
