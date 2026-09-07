using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventProposal> EventProposals => Set<EventProposal>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
    public DbSet<UserNotification> Notifications => Set<UserNotification>();
    public DbSet<UserAccount> Users => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        announcement.HasIndex(item => item.EventId)
            .IsUnique();

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
        tuitionEvent.HasIndex(item => new
        {
            item.CourseId,
            item.Status,
            item.StartsAt
        });

        var proposal = modelBuilder.Entity<EventProposal>();

        proposal.Property(item => item.ProposedRegistrationAudience)
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
        proposal.HasIndex(item => new
        {
            item.ProposedByUserId,
            item.Status
        });

        proposal.HasIndex(item => item.CreatedEventId)
            .IsUnique();

        proposal.HasOne(item => item.CreatedEvent)
            .WithOne(item => item.SourceProposal)
            .HasForeignKey<EventProposal>(item => item.CreatedEventId)
            .OnDelete(DeleteBehavior.SetNull);

        var user = modelBuilder.Entity<UserAccount>();

        user.ToTable("Users");

        user.HasIndex(item => item.Email)
            .IsUnique();

        var notification = modelBuilder.Entity<UserNotification>();

        notification.Property(item => item.Type)
            .HasConversion<string>()
            .HasMaxLength(40);

        notification.HasOne(item => item.User)
            .WithMany(item => item.Notifications)
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}
