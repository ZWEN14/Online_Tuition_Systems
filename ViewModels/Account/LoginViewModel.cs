using System.ComponentModel.DataAnnotations;

namespace Online_Tuition_Systems.ViewModels.Account;

public class LoginViewModel
{
    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
