using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using TranslyAI.Api.Data;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Entities;
using TranslyAI.Api.Enums;
using TranslyAI.Api.Services.IServices;

namespace TranslyAI.Api.Services;

public class AuthService(
    TranslyDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService,
    ILogger<AuthService> logger,
    IMapper mapper) : IAuthService
{

    public async Task<UserDto?> RegisterAsync(
        RegisterRequestDto registerRequest,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(registerRequest.Email);

        if (await IsEmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return null;
        }

        User user = new()
        {
            Email = normalizedEmail,
            UserName = registerRequest.UserName,
            PasswordHash = string.Empty,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, registerRequest.Password);

        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // throw new InvalidOperationException("An error occurred while registering the user", ex);
            return null;
        }

        return mapper.Map<UserDto>(user);
    }

    public async Task<LoginResponseDto?> LoginAsync(
        LoginRequestDto loginRequest,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(loginRequest.Email);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginRequest.Password);

        if (result is PasswordVerificationResult.Success)
        {
            var response = new LoginResponseDto
            {
                Token = await tokenService.IssueAsync(user.Id, cancellationToken),
                User = mapper.Map<UserDto>(user)
            };

            return response;
        }
        else if (result is PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, loginRequest.Password);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Password hash upgraded for user {UserId}.", user.Id);

            var response = new LoginResponseDto
            {
                Token = await tokenService.IssueAsync(user.Id, cancellationToken),
                User = mapper.Map<UserDto>(user)
            };

            return response;
        }
        else if (result is PasswordVerificationResult.Failed)
        {
            return null;
        }
        else
        {
            throw new InvalidOperationException(
                    $"Unhandled {nameof(PasswordVerificationResult)} value: {result}."
                );
        }
    }

    public async Task<bool> IsEmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        return await dbContext.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public Task<UserDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
        => dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserDto
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

    public async Task<DeleteAccountResult> DeleteAccountAsync(Guid userId, string password, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if(user == null)
        {
            return DeleteAccountResult.UserNotFound;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

        if(result is PasswordVerificationResult.Failed)
        {
            return DeleteAccountResult.InvalidPassword;
        }

        dbContext.Users.Remove(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // طلب تاني مسح نفس الحساب قبلنا بلحظة، فالنتيجة المطلوبة حصلت خلاص.
            return DeleteAccountResult.Deleted;
        }

        logger.LogInformation("User {UserId} deleted their account.", userId);

        return DeleteAccountResult.Deleted;
    }
}
