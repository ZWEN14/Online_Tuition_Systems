using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

public class Course
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
}
