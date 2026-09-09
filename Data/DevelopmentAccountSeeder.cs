using Microsoft.EntityFrameworkCore;

namespace Online_Tuition_Systems.Data;

public static class DevelopmentAccountSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var helper = scope.ServiceProvider.GetRequiredService<Helper>();

        var configuredAccounts = new[]
        {
            ReadAccount(configuration, "TestAdmin", UserRole.Admin),
            ReadAccount(configuration, "TestStudent", UserRole.Student),
            ReadAccount(configuration, "TestTutor", UserRole.Tutor)
        }.OfType<ConfiguredAccount>();

        foreach (var account in configuredAccounts)
        {
            var email = account.Email.Trim().ToLowerInvariant();
            var previousEmail = account.PreviousEmail?.Trim().ToLowerInvariant();
            var user = await context.Users.SingleOrDefaultAsync(user => user.Email == email);

            if (user is null && !string.IsNullOrWhiteSpace(previousEmail))
            {
                user = await context.Users.SingleOrDefaultAsync(user => user.Email == previousEmail);
            }

            if (user is null)
            {
                user = new User
                {
                    Name = account.Role.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(user);
            }

            // Development demo accounts are deterministic so every developer
            // can sign in with the credentials in appsettings.Development.json.
            user.Email = email;
            user.Hash = helper.HashPassword(account.Password);
            user.Role = account.Role;
            user.EmailVerified = true;
            user.IsBlocked = false;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await context.SaveChangesAsync();

            if (account.Role == UserRole.Student &&
                !await context.Students.AnyAsync(student => student.UserId == user.Id))
            {
                context.Students.Add(new Student
                {
                    UserId = user.Id,
                    EducationLevel = "Undergraduate"
                });
            }
            else if (account.Role == UserRole.Tutor &&
                     !await context.Tutors.AnyAsync(tutor => tutor.UserId == user.Id))
            {
                context.Tutors.Add(new Tutor
                {
                    UserId = user.Id,
                    Rating = 0
                });
            }

            await context.SaveChangesAsync();
        }
    }

    private static ConfiguredAccount? ReadAccount(
        IConfiguration configuration,
        string section,
        UserRole role)
    {
        var email = configuration[$"{section}:Email"];
        var previousEmail = configuration[$"{section}:PreviousEmail"];
        var password = configuration[$"{section}:Password"];

        return string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password)
            ? null
            : new ConfiguredAccount(email, previousEmail, password, role);
    }

    private sealed record ConfiguredAccount(
        string Email,
        string? PreviousEmail,
        string Password,
        UserRole Role);
}
