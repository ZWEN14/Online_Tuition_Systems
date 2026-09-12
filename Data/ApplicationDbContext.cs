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

    // Survey and Complaint modules.
    public DbSet<SubmissionAttachment> SubmissionAttachments => Set<SubmissionAttachment>();
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<SurveySection> SurveySections => Set<SurveySection>();
    public DbSet<SurveyBranchRule> SurveyBranchRules => Set<SurveyBranchRule>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<SurveyAnswer> SurveyAnswers => Set<SurveyAnswer>();
    public DbSet<ComplaintCategory> ComplaintCategories => Set<ComplaintCategory>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<ComplaintStatusHistory> ComplaintStatusHistories => Set<ComplaintStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<SurveySection>().HasOne(x => x.NextSection).WithMany().HasForeignKey(x => x.NextSectionId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<SubmissionAttachment>().HasOne(x => x.SurveyAnswer).WithMany(x => x.Attachments).HasForeignKey(x => x.SurveyAnswerId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SubmissionAttachment>().HasOne(x => x.Complaint).WithMany(x => x.Attachments).HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SubmissionAttachment>().ToTable(t => t.HasCheckConstraint("CK_SubmissionAttachment_Owner", "([SurveyAnswerId] IS NOT NULL AND [ComplaintId] IS NULL) OR ([SurveyAnswerId] IS NULL AND [ComplaintId] IS NOT NULL)"));
        modelBuilder.Entity<SurveyResponse>().HasIndex(x => new { x.SurveyId, x.UserId }).IsUnique();
        modelBuilder.Entity<SurveyAnswer>().HasIndex(x => new { x.SurveyResponseId, x.QuestionId }).IsUnique();
        modelBuilder.Entity<SurveyAnswer>().HasOne(x => x.Question).WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Question>().HasOne(x => x.Section).WithMany(x => x.Questions).HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<SurveyBranchRule>().HasIndex(x => x.QuestionOptionId).IsUnique();
        modelBuilder.Entity<SurveySection>().HasIndex(x => new { x.SurveyId, x.DisplayOrder }).IsUnique();
        modelBuilder.Entity<SurveyBranchRule>().HasOne(x => x.QuestionOption).WithOne(x => x.BranchRule).HasForeignKey<SurveyBranchRule>(x => x.QuestionOptionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SurveyBranchRule>().HasOne(x => x.DestinationSection).WithMany().HasForeignKey(x => x.DestinationSectionId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ComplaintCategory>().HasIndex(x => x.Name).IsUnique();

        modelBuilder.Entity<Survey>().HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Complaint>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Complaint>().HasOne(x => x.AssignedTutor).WithMany().HasForeignKey(x => x.AssignedTutorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ComplaintStatusHistory>().HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);


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
