using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AnywhereEdureach.Models;

#nullable disable warnings

// View Models ----------------------------------------------------------------

public class LoginVM
{
    [Required]
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [DisplayName("Remember Me")]
    public bool RememberMe { get; set; }

}

public class RegisterVM
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Required]
    [StringLength(100)]
    [EmailAddress]
    [Remote("CheckEmail", "Account", ErrorMessage = "Duplicated {0}.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Please select your education level.")]
    [DisplayName("Education Level")]
    public string EducationLevel { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [Compare("Password")]
    [DataType(DataType.Password)]
    [DisplayName("Confirm Password")]
    public string Confirm { get; set; }

}

public class VerifyEmailVM
{
    [Required, EmailAddress]
    public string Email { get; set; }

    [Required, StringLength(6, MinimumLength = 6)]
    [DisplayName("Verification code")]
    public string Code { get; set; }
}

public class VerifyPasswordVM
{
    [Required, DataType(DataType.Password)]
    [DisplayName("Current password")]
    public string Current { get; set; }
}

public class PasswordChangeVM
{
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    [DisplayName("New password")]
    public string New { get; set; }

    [Required, Compare("New"), DataType(DataType.Password)]
    [DisplayName("Confirm password")]
    public string Confirm { get; set; }
}

public class UpdatePasswordVM
{
    public bool CurrentVerified { get; set; }

    public bool VerificationCodeSent { get; set; }

    [StringLength(6, MinimumLength = 6)]
    [DisplayName("Email verification code")]
    public string? Code { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [DisplayName("Current Password")]
    public string Current { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [DisplayName("New Password")]
    public string New { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [Compare("New")]
    [DataType(DataType.Password)]
    [DisplayName("Confirm Password")]
    public string Confirm { get; set; }
}

public class ProfileUpdateVM
{
    public string Email { get; set; } = "";
    public string? PhotoPath { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [DataType(DataType.Upload)]
    public IFormFile? Photo { get; set; }
}

public class ResetPasswordVM
{
    [Required, StringLength(100), EmailAddress]
    public string Email { get; set; } = "";

    public string? Token { get; set; }

    [StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    [DisplayName("New password")]
    public string? New { get; set; }

    [Compare("New"), DataType(DataType.Password)]
    [DisplayName("Confirm password")]
    public string? Confirm { get; set; }

    public bool VerificationSent { get; set; }
    public bool RequestSent { get; set; }
}

// Admin  -----------------------------------------------------------------

public class UserListVM
{
    public User User { get; set; }
    public string ExtraInfo { get; set; }
}

public class AdminUsersVM
{
    public List<UserListVM> Users { get; set; } = [];
    public string? Query { get; set; }
    public string Sort { get; set; } = "name";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class UserInsertVM
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Required]
    [Remote("checkEmail", "Account", ErrorMessage = "Duplicated {0}.")]
    public string Email { get; set; }

    [Required]
    [DisplayName("Role")]
    public UserRole Role { get; set; }

    [DisplayName("Education Level")]
    public string? EducationLevel { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Compare("Password")]
    [DisplayName("Confirm Password")]
    public string Confirm { get; set; }
}

public class UserUpdateVM
{
    public int Id { get; set; }
    public string Email { get; set; }
    public UserRole Role { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { set; get; }

    [Precision(3, 2)]
    [Range(0, 5)]
    public decimal? Rating { get; set; }

    [DisplayName]
    public string? EducationLevel { get; set; }

}

// Tutor Browsing ---------------------------------------------------------

public class TutorIndexVM
{
    public List<User> Tutors { get; set; } = [];
    public Subject? SelectedSubject { get; set; }
}

public class TutorShowVM
{
    public User Tutor { get; set; }
    public List<Timeslot> AvailableSlots { get; set; } = [];
    public int? SelectedSubjectId { get; set; }
    public Subject? SelectedSubject { get; set; }
}

// Timeslot (Availability) ------------------------------------------------

public class TimeslotInsertVM
{
    [Required]
    [Range(0, 6)]
    [DisplayName("Day of week")]
    public int DayOfWeek { get; set; }

    [Required]
    [DataType(DataType.Time)]
    [DisplayName("Start time")]
    public TimeSpan StartTime { get; set; }

    [Required]
    [DataType(DataType.Time)]
    [DisplayName("End time")]
    public TimeSpan EndTime { get; set; }

    public bool IsActive { get; set; }
}

// Booking -----------------------------------------------------------------

// Drives the multi-step "Create Booking" GET view (tutor -> timeslot -> date).
public class BookingCreateVM
{
    public List<User> Tutors { get; set; } = [];
    public List<Timeslot> Timeslots { get; set; } = [];
    public List<DateTime> AvailableDates { get; set; } = [];

    public int? SelectedSubjectId { get; set; }
    public Subject? SelectedSubject { get; set; }
    public int? SelectedTutorId { get; set; }
    public int? SelectedTimeslotId { get; set; }
}

// Bound from the final POST that actually creates the booking.
public class BookingInsertVM
{
    [Required]
    public int TutorId { get; set; }

    [Required]
    public int SubjectId { get; set; }

    [Required]
    public int TimeslotId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime BookingDate { get; set; }
}

// Subject -----------------------------------------------------------------

public class SubjectListVM
{
    public Subject Subject { get; set; }
    public int TutorCount { get; set; }
}
