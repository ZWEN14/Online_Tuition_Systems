using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AnywhereEdureach.Models;

#nullable disable warnings

public class DB(DbContextOptions options) : DbContext(options)
{
    // DB Sets
    public DbSet<User> Users { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Tutor> Tutors { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<TutorSubject> TutorSubjects { get; set; }
    public DbSet<Timeslot> Timeslots { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------------------------------------------------------------
        // The four relationships below are configured with Fluent API
        // ONLY because Data Annotations have no way to express delete
        // behaviour, and Booking has multiple foreign keys that would
        // otherwise create competing cascade-delete paths into the same
        // table (a Booking is reachable from Users via StudentId,
        // TutorId, AND indirectly via Timeslot -> Booking). SQL Server
        // rejects this at migration time unless the extra paths are set
        // to Restrict. Every other table/column/relationship uses pure
        // Data Annotations.
        // ---------------------------------------------------------------
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Student)
            .WithMany()
            .HasForeignKey(b => b.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Tutor)
            .WithMany()
            .HasForeignKey(b => b.TutorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Timeslot)
            .WithMany()
            .HasForeignKey(b => b.TimeslotId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Subject)
            .WithMany()
            .HasForeignKey(b => b.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// Entity Classes -------------------------------------------------------------

public enum UserRole
{
    Student,
    Tutor,
    Admin,
}

[Index(nameof(Email), IsUnique = true)]
public class User
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(100)]
    public string Email { get; set; }

    [MaxLength(100)]
    public string Hash { get; set; }

    [MaxLength(255)]
    public string? PhotoPath { get; set; }

    public bool EmailVerified { get; set; } = true;
    public string? EmailVerificationHash { get; set; }
    public DateTime? EmailVerificationExpiresAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public bool IsBlocked { get; set; }

    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation Properties - each of these is the ONLY relationship of
    // its kind between User and the related entity, so there is no
    // ambiguity and no Fluent API configuration is needed for them.
    public Student Student { get; set; }
    public Tutor Tutor { get; set; }
    public List<TutorSubject> TutorSubjects { get; set; } = [];
}

// Extra profile data for users with Role == Student.
public class Student
{
    [Key]
    public int Id { get; set; }

    [MaxLength(50)]
    public string EducationLevel { get; set; }

    // Foreign Keys
    public int UserId { get; set; }

    // Navigation Properties
    public User User { get; set; }
}

// Extra profile data for users with Role == Tutor.
public class Tutor
{
    [Key]
    public int Id { get; set; }

    [Precision(3, 2)]
    public decimal Rating { get; set; }

    // Foreign Keys
    public int UserId { get; set; }

    // Navigation Properties
    public User User { get; set; }
}

// NOTE: Full Subject Maintenance CRUD (Insert/Update/Delete) belongs to
// another team member. This entity is defined here because the
// Tutor/Timeslot/Booking module depends on it.
public class Subject
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    [Precision(10, 2)]
    public decimal BaseCost { get; set; }

    // Navigation Properties
    public List<TutorSubject> TutorSubjects { get; set; } = [];
}

// Many-to-many join between a tutor (a User with Role == Tutor) and the
// subjects that tutor teaches.
public class TutorSubject
{
    [Key]
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Foreign Keys
    public int TutorId { get; set; }
    public int SubjectId { get; set; }

    // Navigation Properties
    public User Tutor { get; set; }
    public Subject Subject { get; set; }
}

// A recurring weekly availability slot that a tutor (a User with
// Role == Tutor) can be booked into. DayOfWeek follows System.DayOfWeek
// convention: 0 = Sunday ... 6 = Saturday.
public class Timeslot
{
    [Key]
    public int Id { get; set; }

    [Range(0, 6)]
    public int DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    // Foreign Keys
    public int TutorId { get; set; }

    // Navigation Properties
    public User Tutor { get; set; }

    // Only active timeslots can be booked by students.
    public bool IsActive { get; set; }
}

public enum BookingStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Cancelled = 3,
    Completed = 4,
    Paid = 5,
}

[Index(nameof(TimeslotId), nameof(BookingDate))]
public class Booking
{
    [Key]
    public int Id { get; set; }

    public DateTime BookingDate { get; set; }

    [Precision(10, 2)]
    public decimal Cost { get; set; }
    public BookingStatus Status { get; set; }

    [MaxLength(255)]
    public string? TransactionId { get; set; }

    [MaxLength(20)]
    public string? PaymentStatus { get; set; }

    public DateTime? PaidAt { get; set; }
    public DateTime? PaymentDeadline { get; set; }
    public DateTime? StudentConfirmedAt { get; set; }
    public DateTime? TutorConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Foreign Keys
    public int StudentId { get; set; }
    public int TutorId { get; set; }
    public int SubjectId { get; set; }
    public int TimeslotId { get; set; }

    // Navigation Properties
    public User Student { get; set; }
    public User Tutor { get; set; }
    public Subject Subject { get; set; }
    public Timeslot Timeslot { get; set; }
}

public class Notification
{
    [Key]
    public int Id { get; set; }

    [MaxLength(255)]
    public string Message { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Foreign Keys
    public int UserId { get; set; }
    public int? BookingId { get; set; }

    // Navigation Properties
    public User User { get; set; }
    public Booking Booking { get; set; }
}
