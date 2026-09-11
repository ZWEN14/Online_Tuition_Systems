using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

public class ComplaintStatusHistory
{
    public int Id { get; set; }
    [Required] public int ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }
    public ComplaintStatus Status { get; set; }
    [StringLength(1000)] public string? Note { get; set; }
    [Required] public int UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
