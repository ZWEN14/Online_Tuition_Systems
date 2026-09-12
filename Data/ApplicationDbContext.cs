using AnywhereEdureach.Models;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Integrated teammate modules.
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventProposal> EventProposals => Set<EventProposal>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
    public DbSet<UserNotification> Notifications => Set<UserNotification>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Tutor> Tutors => Set<Tutor>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TutorSubject> TutorSubjects => Set<TutorSubject>();
    public DbSet<Timeslot> Timeslots => Set<Timeslot>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Notification> BookingNotifications => Set<Notification>();

    // Course Management and Billing modules.
    public DbSet<CourseCategory> CourseCategories => Set<CourseCategory>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Promotion> Promotions => Set<Promotion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureAnnouncementsAndEvents(modelBuilder);
        ConfigureIntegratedUsersAndBookings(modelBuilder);
        ConfigureCourseAndBilling(modelBuilder);
    }

    private static void ConfigureAnnouncementsAndEvents(ModelBuilder modelBuilder)
    {
        var announcement = modelBuilder.Entity<Announcement>();

        announcement.Property(item => item.Audience)
            .HasConversion<string>()
            .HasMaxLength(20);
        announcement.Property(item => item.Priority)
            .HasConversion<string>()
            .HasMaxLength(20);
        announcement.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        announcement.HasIndex(item => item.Status);
        announcement.HasIndex(item => new { item.Audience, item.Status });
        announcement.HasIndex(item => new { item.CourseId, item.Status });
        announcement.HasIndex(item => new
        {
            item.Status,
            item.PublishedAt,
            item.ExpiresAt
        });
        announcement.HasIndex(item => item.EventId).IsUnique();
        announcement.HasOne(item => item.Event)
            .WithOne(item => item.Announcement)
            .HasForeignKey<Announcement>(item => item.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        var tuitionEvent = modelBuilder.Entity<Event>();
        tuitionEvent.Property(item => item.RegistrationAudience)
            .HasConversion<string>()
            .HasMaxLength(20);
        tuitionEvent.Property(item => item.Mode)
            .HasConversion<string>()
            .HasMaxLength(20);
        tuitionEvent.Property(item => item.MeetingPlatform)
            .HasConversion<string>()
            .HasMaxLength(30);
        tuitionEvent.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        tuitionEvent.HasIndex(item => item.Status);
        tuitionEvent.HasIndex(item => item.RegistrationAudience);
        tuitionEvent.HasIndex(item => item.OrganizerUserId);
        tuitionEvent.HasIndex(item => new
        {
            item.CourseId,
            item.Status,
            item.StartsAt
        });

        var proposal = modelBuilder.Entity<EventProposal>();
        proposal.Property(item => item.RegistrationAudience)
            .HasConversion<string>()
            .HasMaxLength(20);
        proposal.Property(item => item.Mode)
            .HasConversion<string>()
            .HasMaxLength(20);
        proposal.Property(item => item.MeetingPlatform)
            .HasConversion<string>()
            .HasMaxLength(30);
        proposal.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        proposal.HasIndex(item => item.Status);
        proposal.HasIndex(item => new { item.ProposedByUserId, item.Status });
        proposal.HasIndex(item => item.CreatedEventId).IsUnique();
        proposal.HasOne(item => item.CreatedEvent)
            .WithOne(item => item.SourceProposal)
            .HasForeignKey<EventProposal>(item => item.CreatedEventId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureIntegratedUsersAndBookings(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();
        user.ToTable("Users");
        user.HasIndex(item => item.Email).IsUnique();

        var notification = modelBuilder.Entity<UserNotification>();
        notification.Property(item => item.Type)
            .HasConversion<string>()
            .HasMaxLength(40);
        notification.HasOne(item => item.User)
            .WithMany()
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Booking>()
            .HasOne(item => item.Student)
            .WithMany()
            .HasForeignKey(item => item.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Booking>()
            .HasOne(item => item.Tutor)
            .WithMany()
            .HasForeignKey(item => item.TutorId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Booking>()
            .HasOne(item => item.Timeslot)
            .WithMany()
            .HasForeignKey(item => item.TimeslotId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Booking>()
            .HasOne(item => item.Subject)
            .WithMany()
            .HasForeignKey(item => item.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureCourseAndBilling(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>()
            .HasOne(course => course.Tutor)
            .WithMany()
            .HasForeignKey(course => course.TutorId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Course>()
            .HasOne(course => course.ReviewedBy)
            .WithMany()
            .HasForeignKey(course => course.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Course>()
            .HasOne(course => course.Category)
            .WithMany(category => category.Courses)
            .HasForeignKey(course => course.CourseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrollment>()
            .HasOne(enrollment => enrollment.Student)
            .WithMany()
            .HasForeignKey(enrollment => enrollment.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Enrollment>()
            .HasOne(enrollment => enrollment.Course)
            .WithMany(course => course.Enrollments)
            .HasForeignKey(enrollment => enrollment.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(payment => payment.User)
            .WithMany()
            .HasForeignKey(payment => payment.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Payment>()
            .HasOne(payment => payment.Enrollment)
            .WithMany(enrollment => enrollment.Payments)
            .HasForeignKey(payment => payment.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Invoice>()
            .HasOne(invoice => invoice.Payment)
            .WithOne(payment => payment.Invoice)
            .HasForeignKey<Invoice>(invoice => invoice.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Promotion>()
            .HasOne(promotion => promotion.Course)
            .WithMany(course => course.Promotions)
            .HasForeignKey(promotion => promotion.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
