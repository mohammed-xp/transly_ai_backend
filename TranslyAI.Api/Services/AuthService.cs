using System.Data.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using TranslyAI.Api.Data;
using TranslyAI.Api.Entities;

namespace TranslyAI.Api.Services;

public class AuthService(
    TranslyDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    ILogger<AuthService> logger)
{

    public async Task<User?> RegisterAsync(
        string email,
        string userName,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (await dbContext.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            return null;
        }

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = normalizedEmail,
            UserName = userName,
            PasswordHash = string.Empty,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstrainViolation(ex))
        {
            logger.LogInformation("Gegister race lost for an already-registered email.");
            return null;
        }

        return user;
    }

    private static string NormalizeEmail(string email)
    => email.Trim().ToLowerInvariant();

    private static bool IsUniqueConstrainViolation(DbUpdateException exception)
        => exception.InnerException is DbException { SqlState: "23000" or "23505" }
        || exception.InnerException is MySqlException {Number: 1062};
}