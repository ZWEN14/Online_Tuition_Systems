using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using AnywhereEdureach.Services;

namespace AnywhereEdureach.Controllers;

public class AccountController(ApplicationDbContext db, Helper hp, IWebHostEnvironment env, ILogger<AccountController> logger, GoogleRecaptchaService recaptcha, IEmailSender emailSender) : Controller
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private const int MaxPhotoBytes = 5 * 1024 * 1024;

    // GET: Account/Login
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        ViewBag.RecaptchaSiteKey = recaptcha.SiteKey;
        return View();
    }

    // POST: Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVM vm, string? returnURL, [FromForm(Name = "g-recaptcha-response")] string? captchaToken)
    {
        captchaToken ??= Request.Form["g-recaptcha-response"].FirstOrDefault();
        if (!ModelState.IsValid)
        {
            ViewBag.RecaptchaSiteKey = recaptcha.SiteKey;
            return View(vm);
        }

        var u = db.Users.FirstOrDefault(u => u.Email == vm.Email);

        if (!await recaptcha.VerifyAsync(captchaToken, HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.RequestAborted))
        {
            ModelState.AddModelError("Captcha", "Please complete the CAPTCHA check.");
        }

        if (u?.IsBlocked == true)
        {
            ModelState.AddModelError(string.Empty, "This account has been blocked. Please contact an administrator.");
        }
        else if (u?.LockoutEnd > DateTime.UtcNow)
        {
            ModelState.AddModelError(string.Empty, "This account is temporarily unavailable. Please try again later.");
        }
        else if (u == null || !hp.VerifyPassword(u.Hash, vm.Password))
        {
            ModelState.AddModelError("", "Login credentials not matched.");
            if (u != null)
            {
                u.FailedLoginAttempts++;
                if (u.FailedLoginAttempts >= 5)
                {
                    u.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                    u.FailedLoginAttempts = 0;
                    ModelState.AddModelError(string.Empty, "Too many failed attempts. Try again in 15 minutes.");
                }
                db.SaveChanges();
            }
        }

        if (ModelState.IsValid)
        {
            u!.FailedLoginAttempts = 0;
            u.LockoutEnd = null;
            db.SaveChanges();

            if (!u.EmailVerified)
            {
                TempData["Info"] = "Please verify your email before signing in.";
                return RedirectToAction(nameof(VerifyEmail), new { email = u.Email });
            }

            TempData["Info"] = "Login successfully.";

            // (3) Sign in
            hp.SignIn(u!.Id, u.Email, u.Name, u.Role.ToString(), vm.RememberMe);

            // (4) Handle return URL
            if (!string.IsNullOrEmpty(returnURL) && Url.IsLocalUrl(returnURL))
            {
                return Redirect(returnURL);
            }

            return RedirectToAction("Index", "Home");
        }

        ViewBag.RecaptchaSiteKey = recaptcha.SiteKey;
        return View(vm);
    }

    [Authorize]
    public IActionResult PhotoEditor()
    {
        var u = db.Users.Find(CurrentUserId);
        if (u == null) return RedirectToAction("Index", "Home");

        return View(new ProfileUpdateVM
        {
            Email = u.Email,
            Name = u.Name,
            PhotoPath = u.PhotoPath
        });
    }

    // POST: Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout(string? returnURL)
    {
        TempData["Info"] = "Logout successfully.";

        hp.SignOut();

        return RedirectToAction("Welcome", "Home");
    }

    // GET: Account/AccessDenied
    public IActionResult AccessDenied(string? returnURL)
    {
        return View();
    }



    // ------------------------------------------------------------------------
    // Others
    // ------------------------------------------------------------------------

    // GET: Account/CheckEmail
    public async Task<bool> CheckEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return true;
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        return !await db.Users.AnyAsync(u => u.Email == normalizedEmail);
    }

    // GET: Account/Register
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");

        ViewBag.EducationLevel = new SelectList(Helper.EducationLevels);
        ViewBag.RecaptchaSiteKey = recaptcha.SiteKey;
        return View();
    }

    // POST: Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVM vm, [FromForm(Name = "g-recaptcha-response")] string? captchaToken)
    {
        captchaToken ??= Request.Form["g-recaptcha-response"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(vm.Email))
        {
            vm.Email = vm.Email.Trim().ToLowerInvariant();
        }

        if (ModelState.IsValid("Email") &&
            await db.Users.AnyAsync(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
        }

        if (!await recaptcha.VerifyAsync(captchaToken, HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.RequestAborted))
        {
            ModelState.AddModelError("Captcha", "Please complete the CAPTCHA check.");
        }

        if (ModelState.IsValid)
        {
            var u = new User
            {
                Name = vm.Name,
                Email = vm.Email,
                Hash = hp.HashPassword(vm.Password),
                Role = UserRole.Student,
                EmailVerified = false,
            };
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            u.EmailVerificationHash = hp.HashPassword(code);
            u.EmailVerificationExpiresAt = DateTime.UtcNow.AddMinutes(10);
            db.Users.Add(u);
            db.Students.Add(new Student
            {
                User = u,
                EducationLevel = vm.EducationLevel,
            });

            try
            {
                // EF Core wraps both INSERT statements in one transaction.
                // A duplicate email or profile failure cannot leave a partial account.
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                db.ChangeTracker.Clear();
                ModelState.AddModelError(nameof(vm.Email), "An account with this email already exists.");
                ViewBag.EducationLevel = new SelectList(Helper.EducationLevels);
                ViewBag.RecaptchaSiteKey = recaptcha.SiteKey;
                return View(vm);
            }

            var emailSent = await SendVerificationCodeAsync(u.Email, code, "Verify your Anywhere Edureach account");
            TempData[emailSent ? "Info" : "Error"] = emailSent
                ? "Registration complete. A verification code was sent to your email."
                : "Registration complete, but the verification email could not be sent. Check the SMTP settings and use Resend code.";
            return RedirectToAction(nameof(VerifyEmail), new { email = u.Email });
        }

        ViewBag.EducationLevel = new SelectList(Helper.EducationLevels);
        ViewBag.RecaptchaSiteKey = recaptcha.SiteKey;
        return View(vm);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    [HttpGet]
    public IActionResult VerifyEmail(string email) => View(new VerifyEmailVM { Email = email });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VerifyEmail(VerifyEmailVM vm)
    {
        var u = db.Users.FirstOrDefault(x => x.Email == vm.Email);
        if (u == null || u.EmailVerificationExpiresAt < DateTime.UtcNow || string.IsNullOrEmpty(u.EmailVerificationHash) || !hp.VerifyPassword(u.EmailVerificationHash, vm.Code))
        {
            ModelState.AddModelError(nameof(vm.Code), "The verification code is invalid or expired.");
        }

        if (!ModelState.IsValid) return View(vm);

        u!.EmailVerified = true;
        u.EmailVerificationHash = null;
        u.EmailVerificationExpiresAt = null;
        db.SaveChanges();
        TempData["Info"] = "Email verified. You can now sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendVerification(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "The email address is missing. Please register again.";
            return RedirectToAction(nameof(Register));
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail);

        if (user is not null && !user.EmailVerified)
        {
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            user.EmailVerificationHash = hp.HashPassword(code);
            user.EmailVerificationExpiresAt = DateTime.UtcNow.AddMinutes(10);
            await db.SaveChangesAsync();

            var emailSent = await SendVerificationCodeAsync(user.Email, code, "Verify your Anywhere Edureach account");
            TempData[emailSent ? "Info" : "Error"] = emailSent
                ? "A new verification code was sent to your email."
                : "The verification email could not be sent. Check the SMTP settings and try again.";
        }
        else
        {
            TempData["Info"] = "If this account still requires verification, a new code will be sent.";
        }

        return RedirectToAction(nameof(VerifyEmail), new { email = normalizedEmail });
    }

    // GET: Account/UpdatePassword
    [Authorize]
    public IActionResult UpdatePassword()
    {
        return View(new UpdatePasswordVM());
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> VerifyPassword(UpdatePasswordVM vm)
    {
        ModelState.Clear();
        var u = db.Users.Find(CurrentUserId);
        if (u == null || !hp.VerifyPassword(u.Hash, vm.Current))
        {
            ModelState.AddModelError(nameof(vm.Current), "Current password not matched.");
            return View("UpdatePassword", vm);
        }

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        u.EmailVerificationHash = hp.HashPassword(code);
        u.EmailVerificationExpiresAt = DateTime.UtcNow.AddMinutes(10);
        db.SaveChanges();
        var emailSent = await SendVerificationCodeAsync(u.Email, code, "Verify your password change");
        if (!emailSent)
        {
            TempData["Error"] = "The verification email could not be sent. Check the SMTP settings and try again.";
            return View("UpdatePassword", new UpdatePasswordVM());
        }

        TempData["PasswordVerified"] = true;
        TempData["Info"] = "A verification code was sent to your email.";
        return View("UpdatePassword", new UpdatePasswordVM { CurrentVerified = true, VerificationCodeSent = true });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdatePassword(UpdatePasswordVM vm)
    {
        var u = db.Users.Find(CurrentUserId);
        if (u == null) return RedirectToAction("Index", "Home");

        if (TempData.Peek("PasswordVerified") is not true)
        {
            return RedirectToAction(nameof(UpdatePassword));
        }

        ModelState.Remove(nameof(vm.Current));
        if (string.IsNullOrWhiteSpace(vm.Code) || u.EmailVerificationExpiresAt < DateTime.UtcNow ||
            string.IsNullOrEmpty(u.EmailVerificationHash) || !hp.VerifyPassword(u.EmailVerificationHash, vm.Code))
        {
            ModelState.AddModelError(nameof(vm.Code), "The verification code is invalid or expired.");
        }

        if (ModelState.IsValid)
        {
            u.Hash = hp.HashPassword(vm.New);
            u.EmailVerificationHash = null;
            u.EmailVerificationExpiresAt = null;
            db.SaveChanges();

            TempData["Info"] = "Password updated.";
            return RedirectToAction(nameof(UpdateProfile));
        }

        vm.CurrentVerified = true;
        vm.VerificationCodeSent = true;
        return View(vm);
    }

    // GET: Account/UpdateProfile
    [Authorize]
    public IActionResult UpdateProfile()
    {
        var u = db.Users.Find(CurrentUserId);
        if (u == null) return RedirectToAction("Index", "Home");

        var vm = new ProfileUpdateVM
        {
            Email = u.Email,
            Name = u.Name,
            PhotoPath = u.PhotoPath,
        };

        return View(vm);
    }

    // POST: Account/UpdateProfile
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateProfile([Bind("Name,Photo")] ProfileUpdateVM vm)
    {
        var u = db.Users.Find(CurrentUserId);
        if (u == null) return RedirectToAction("Index", "Home");

        if (ModelState.IsValid)
        {
            var name = vm.Name.Trim();
            string? photoPath = null;
            if (vm.Photo != null)
            {
                photoPath = SavePhoto(vm.Photo, u.PhotoPath);
                if (photoPath == null)
                {
                    vm.Email = u.Email;
                    vm.PhotoPath = u.PhotoPath;
                    return View(vm);
                }
            }

            var affectedRows = photoPath == null
                ? db.Users
                    .Where(user => user.Id == CurrentUserId)
                    .ExecuteUpdate(setters => setters.SetProperty(user => user.Name, name))
                : db.Users
                    .Where(user => user.Id == CurrentUserId)
                    .ExecuteUpdate(setters => setters
                        .SetProperty(user => user.Name, name)
                        .SetProperty(user => user.PhotoPath, photoPath));

            if (affectedRows != 1)
            {
                TempData["Info"] = "The profile was not changed because the account record could not be updated.";
                return RedirectToAction(nameof(UpdateProfile));
            }

            hp.SignIn(u.Id, u.Email, name, u.Role.ToString(), true);

            TempData["Info"] = "Profile updated.";
            return RedirectToAction(nameof(UpdateProfile));
        }

        vm.Email = u.Email;
        vm.PhotoPath = u.PhotoPath;
        return View(vm);
    }

    [Authorize]
    [HttpPost]
    public IActionResult RotatePhoto(int degrees = 90) => RedirectToAction(nameof(UpdateProfile));

    private async Task<bool> SendVerificationCodeAsync(string email, string code, string subject)
    {
        try
        {
            await emailSender.SendAsync(email, subject, $"Your Anywhere Edureach verification code is {code}. It expires in 10 minutes.", HttpContext.RequestAborted);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to send verification email to {Email}.", email);
            return false;
        }
    }

    // GET: Account/ResetPassword
    public IActionResult ResetPassword(string? email, string? token)
    {
        return View(new ResetPasswordVM
        {
            Email = email ?? "",
            Token = token,
            VerificationSent = !string.IsNullOrWhiteSpace(token)
        });
    }

    // POST: Account/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordVM vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Token))
        {
            if (ModelState.IsValid)
            {
                var u = db.Users.FirstOrDefault(x => x.Email == vm.Email);
                if (u != null)
                {
                    var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                        .Replace('+', '-').Replace('/', '_').TrimEnd('=');
                    u.EmailVerificationHash = hp.HashPassword(token);
                    u.EmailVerificationExpiresAt = DateTime.UtcNow.AddMinutes(15);
                    db.SaveChanges();

                    var link = Url.Action(nameof(ResetPassword), "Account", new { email = u.Email, token }, Request.Scheme);
                    await SendResetEmailAsync(u.Email, link!);
                }

                return View(new ResetPasswordVM { Email = vm.Email, RequestSent = true });
            }

            return View(vm);
        }

        var user = db.Users.FirstOrDefault(x => x.Email == vm.Email);
        if (string.IsNullOrWhiteSpace(vm.New))
        {
            ModelState.AddModelError(nameof(vm.New), "Please enter a new password.");
        }
        if (string.IsNullOrWhiteSpace(vm.Confirm))
        {
            ModelState.AddModelError(nameof(vm.Confirm), "Please confirm the new password.");
        }
        if (user == null || user.EmailVerificationExpiresAt < DateTime.UtcNow ||
            string.IsNullOrWhiteSpace(user.EmailVerificationHash) || !hp.VerifyPassword(user.EmailVerificationHash, vm.Token))
        {
            ModelState.AddModelError(nameof(vm.Token), "The reset link is invalid or expired.");
        }

        if (!ModelState.IsValid)
        {
            vm.VerificationSent = true;
            return View(vm);
        }

        if (user is null)
        {
            return NotFound();
        }

        user.Hash = hp.HashPassword(vm.New!);
        user.EmailVerificationHash = null;
        user.EmailVerificationExpiresAt = null;
        db.SaveChanges();
        TempData["Info"] = "Password reset successfully. You can now sign in.";
        return RedirectToAction(nameof(Login));
    }

    private async Task SendResetEmailAsync(string email, string link)
    {
        try
        {
            await emailSender.SendAsync(email, "Reset your Anywhere Edureach password",
                $"Use this link to reset your Anywhere Edureach password:\n\n{link}\n\nThis link expires in 15 minutes and can only be used once.",
                HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to send password reset email to {Email}.", email);
        }
    }

    private string? SavePhoto(IFormFile photo, string? existingPath)
    {
        if (photo.Length == 0 || photo.Length > MaxPhotoBytes || !new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(Path.GetExtension(photo.FileName).ToLowerInvariant()))
        {
            ModelState.AddModelError(nameof(ProfileUpdateVM.Photo), "Use a JPG, PNG, or WebP image up to 5 MB.");
            return null;
        }

        var directory = Path.Combine(env.WebRootPath, "uploads", "profiles");
        Directory.CreateDirectory(directory);
        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(photo.FileName).ToLowerInvariant()}";
        var fullPath = Path.Combine(directory, fileName);
        using var stream = System.IO.File.Create(fullPath);
        photo.CopyTo(stream);

        if (!string.IsNullOrEmpty(existingPath))
        {
            var oldFile = Path.Combine(env.WebRootPath, existingPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(oldFile)) System.IO.File.Delete(oldFile);
        }

        return $"/uploads/profiles/{fileName}";
    }
}
