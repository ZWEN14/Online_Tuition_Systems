using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

// Tracks the submitter, category, optional assigned tutor and current status.
public class Complaint
{
    public ICollection<SubmissionAttachment> Attachments { get; set; } = new List<SubmissionAttachment>();
    public int Id { get; set; }
    [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(3000)] public string Description { get; set; } = string.Empty;
    [Required, Display(Name = "Category")] public int CategoryId { get; set; }
    public ComplaintCategory? Category { get; set; }
    [Required] public int UserId { get; set; }
    public User? User { get; set; }
    [Display(Name = "Assigned Tutor")] public int? AssignedTutorId { get; set; }
    public User? AssignedTutor { get; set; }
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;
    [Display(Name = "Resolution Notes"), StringLength(3000)] public string? ResolutionNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ComplaintStatusHistory> StatusHistory { get; set; } = new List<ComplaintStatusHistory>();
}
