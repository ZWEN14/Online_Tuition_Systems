using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

public class Enrollment
{
    public int Id { get; set; }
    [Required] public int UserId { get; set; }
    public User? User { get; set; }
    [Required] public int CourseId { get; set; }
    public Course? Course { get; set; }
}
