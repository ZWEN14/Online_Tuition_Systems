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
            if (await context.Users.AnyAsync(user => user.Email == email))
            {
                continue;
            }

            var previousEmail = account.PreviousEmail?.Trim().ToLowerInvariant();
            var existingUser = string.IsNullOrWhiteSpace(previousEmail)
                ? null
                : await context.Users.SingleOrDefaultAsync(user => user.Email == previousEmail);

            if (existingUser is not null)
            {
                existingUser.Email = email;
                existingUser.Hash = helper.HashPassword(account.Password);
                existingUser.Role = account.Role;
                existingUser.EmailVerified = true;
                await context.SaveChangesAsync();
                continue;
            }

            var user = new User
            {
                Name = account.Role.ToString(),
                Email = email,
                Hash = helper.HashPassword(account.Password),
                Role = account.Role,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            if (account.Role == UserRole.Student)
            {
                context.Students.Add(new Student
                {
                    UserId = user.Id,
                    EducationLevel = "Undergraduate"
                });
            }
            else if (account.Role == UserRole.Tutor)
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
