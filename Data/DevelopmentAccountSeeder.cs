using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Authorization;
using Online_Tuition_Systems.Models;

namespace Online_Tuition_Systems.Data;

public static class DevelopmentAccountSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<UserAccount>>();

        var configuredAccounts = new[]
        {
            ReadAccount(configuration, "TestAdmin", AppRoles.Admin),
            ReadAccount(configuration, "TestStudent", AppRoles.Student),
            ReadAccount(configuration, "TestTutor", AppRoles.Tutor)
        }.OfType<ConfiguredAccount>();

        foreach (var configuredAccount in configuredAccounts)
        {
            var email = NormalizeEmail(configuredAccount.Email);
            var accountExists = await context.Users
                .AnyAsync(user => user.Email == email);

            if (accountExists)
            {
                continue;
            }

            var user = new UserAccount
            {
                Email = email,
                Role = configuredAccount.Role,
                CreatedAt = DateTimeOffset.UtcNow
            };

            user.PasswordHash = passwordHasher.HashPassword(
                user,
                configuredAccount.Password);

            context.Users.Add(user);
        }

        await context.SaveChangesAsync();
    }

    private static ConfiguredAccount? ReadAccount(
        IConfiguration configuration,
        string section,
        string role)
    {
        var email = configuration[$"{section}:Email"];
        var password = configuration[$"{section}:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        return new ConfiguredAccount(email, password, role);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private sealed record ConfiguredAccount(
        string Email,
        string Password,
        string Role);
}
