using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace AnywhereEdureach;

public class Helper(IHttpContextAccessor ha)
{
    public static readonly string[] EducationLevels =
    [
        "Secondary 1", "Secondary 2", "Secondary 3", "Secondary 4", "Secondary 5",
        "Foundation / Pre-University", "Diploma", "Undergraduate"
    ];

    // ------------------------------------------------------------------------
    // Security Helper Functions
    // ------------------------------------------------------------------------

    private readonly PasswordHasher<object> ph = new();

    public string HashPassword(string password)
    {
        return ph.HashPassword(0, password);
    }

    public bool VerifyPassword(string hash, string password)
    {
        return ph.VerifyHashedPassword(0, hash, password)
               == PasswordVerificationResult.Success;
    }

    // NOTE: adapted from the practical's SignIn(email, role, rememberMe) -
    // our schema uses an int Id as the primary key (not Email), so the id
    // is carried as an extra claim (ClaimTypes.NameIdentifier) for
    // controllers that need to look up "my own" records efficiently.
    public void SignIn(int id, string email, string name, string role, bool rememberMe)
    {
        // (1) Claim, identity and principal
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, id.ToString()),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
        ];

        ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        ClaimsPrincipal principal = new(identity);

        // (2) Remember me (authentication properties)
        AuthenticationProperties properties = new()
        {
            IsPersistent = rememberMe,
        };

        // (3) Sign in
        ha.HttpContext!.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }

    public void SignOut()
    {
        ha.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

}
