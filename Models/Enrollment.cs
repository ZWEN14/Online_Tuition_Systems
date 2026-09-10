using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Online_Tuition_Systems.Models;

[Table("Enrollments")]
[Index(nameof(StudentId), nameof(CourseId), IsUnique = true)]
[Index(nameof(Status))]
public class Enrollment
{
    [Key]
    public int EnrollmentId { get; set; }

    [Required]
    public int StudentId { get; set; }

    [Required]
    public int CourseId { get; set; }

    [Required]
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.PendingPayment;

    [Column(TypeName = "datetime2")]
    public DateTime EnrolledAtUtc { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime? ActivatedAtUtc { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime? CancelledAtUtc { get; set; }

    [ForeignKey(nameof(StudentId))]
    public User Student { get; set; } = null!;

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;

    public ICollection<Payment> Payments { get; set; } = [];
}
