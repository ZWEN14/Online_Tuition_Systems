using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Data;
using Online_Tuition_Systems.Models;
using Online_Tuition_Systems.Services.Security;
using Online_Tuition_Systems.ViewModels.Account;

namespace Online_Tuition_Systems.Controllers;

public class AccountController(
    ApplicationDbContext dbContext,
    IPasswordHasher passwordHasher) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Email == normalizedEmail);

        if (user is null
            || !user.IsActive
            || !passwordHasher.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        await SignInAsync(user, model.RememberMe);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl)
            && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role is not UserRole.Student and not UserRole.Tutor)
        {
            ModelState.AddModelError(nameof(model.Role), "Select Student or Tutor.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(user => user.Email == normalizedEmail))
        {
            ModelState.AddModelError(nameof(model.Email), "An account already uses this email address.");
            return View(model);
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            InstitutionId = GenerateInstitutionId(model.Role, now),
            Name = model.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(model.Password),
            Role = model.Role,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The account could not be created. Check the details and try again.");
            return View(model);
        }

        await SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> SetupAdministrator()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        if (await dbContext.Users.AnyAsync())
        {
            return NotFound();
        }

        return View(new AdministratorSetupViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetupAdministrator(
        AdministratorSetupViewModel model)
    {
        if (await dbContext.Users.AnyAsync())
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var now = DateTime.UtcNow;
        var administrator = new User
        {
            InstitutionId = GenerateInstitutionId(UserRole.Administrator, now),
            Name = model.Name.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHasher.Hash(model.Password),
            Role = UserRole.Administrator,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.Users.Add(administrator);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Administrator setup could not be completed. Reload the page and try again.");
            return View(model);
        }

        await SignInAsync(administrator, isPersistent: false);
        TempData["SuccessMessage"] = "Administrator account created successfully.";
        return RedirectToAction("Index", "AdminCourseCategories");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SignInAsync(User user, bool isPersistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("InstitutionId", user.InstitutionId)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                AllowRefresh = true
            });
    }

    private static string GenerateInstitutionId(UserRole role, DateTime createdAtUtc)
    {
        var prefix = role switch
        {
            UserRole.Administrator => "ADM",
            UserRole.Tutor => "TUT",
            _ => "STU"
        };
        var randomPart = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        return $"{prefix}-{createdAtUtc:yyyy}-{randomPart}";
    }
}
