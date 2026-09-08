using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using TranslyAI.Api.Data;
using TranslyAI.Api.Dtos;
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
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            logger.LogInformation("Register race lost for an already-registered email.");
            return null;
        }

        return user;
    }

    public async Task<User?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

        switch (result)
        {
            case PasswordVerificationResult.Success:
                return user;

            case PasswordVerificationResult.SuccessRehashNeeded:
                user.PasswordHash = passwordHasher.HashPassword(user, password);
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Password hash upgraded for user {UserId}.", user.Id);
                return user;

            case PasswordVerificationResult.Failed:
                return null;

            default:
                throw new InvalidOperationException(
                    $"Unhandled {nameof(PasswordVerificationResult)} value: {result}."
                );
        }
    }

    public Task<UserResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
        => dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Email = u.Email,
                UserName = u.UserName,
                CreatedAt = new DateTimeOffset(u.CreatedAtUtc, TimeSpan.Zero)
            })
            .FirstOrDefaultAsync(cancellationToken);

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        => exception.InnerException is MySqlException { Number: 1062 };
}