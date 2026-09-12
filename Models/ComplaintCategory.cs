using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.Models;

public class ComplaintCategory
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
