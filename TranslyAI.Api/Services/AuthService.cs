using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using TranslyAI.Api.Data;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Entities;
using TranslyAI.Api.Enums;
using TranslyAI.Api.Services.IServices;
using TranslyAI.Api.Services.Result;

namespace TranslyAI.Api.Services;

public class AuthService(
    TranslyDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokenService,
    IGoogleTokenValidator googleTokenValidator,
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
            EmailVerified = false,
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

        if (user is null || user.PasswordHash is null)
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

    public async Task<LoginResponseDto?> LoginWithGoogleAsync(string idToken, CancellationToken cancellationToken)
    {
        var google = await googleTokenValidator.ValidateAsync(idToken, cancellationToken);

        if(google is null)
        {
            return null;
        }

        var user = await FindOrCreateGoogleUserAsync(google, cancellationToken);

        if(user is null)
        {
            return null;
        }

        return new LoginResponseDto
        {
            Token = await tokenService.IssueAsync(user.Id, cancellationToken),
            User = mapper.Map<UserDto>(user)
        };
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

    public async Task<DeleteAccountResult> DeleteAccountAsync(Guid userId, DeleteAccountRequestDto request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return DeleteAccountResult.UserNotFound;
        }

        if (!string.IsNullOrEmpty(request.IdToken))
        {
            var google = await googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken);

            if(google is null || google.Subject != user.GoogleSubject)
            {
                return DeleteAccountResult.InvalidGoogleToken;
            }
        }
        else
        {
            if(user.PasswordHash is null
                || string.IsNullOrEmpty(request.Password)
                || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                    is PasswordVerificationResult.Failed)
            {
                return DeleteAccountResult.InvalidPassword;
            }
        }

        dbContext.Users.Remove(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // طلب تاني مسح نفس الحساب قبلنا بلحظة، فالنتيجة المطلوبة حصلت خلاص.
            return DeleteAccountResult.Deleted;
        }

        logger.LogInformation("User {UserId} deleted their account.", userId);

        return DeleteAccountResult.Deleted;
    }

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        => exception.InnerException is MySqlException { Number: 1062 };

    private async Task<User?> FindOrCreateGoogleUserAsync(
        GoogleUserInfo google,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(google.Email);

        for (var attempt=1; ; attempt++)
        {
            // الحالة 1: دخل بجوجل قبل كده
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.GoogleSubject == google.Subject, cancellationToken);

            if(user is not null)
            {
                return user;
            }

            // الحالة 2: عنده حساب بنفس الإيميل = نربطه بجوجل
            user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if(user is not null)
            {
                if(user.GoogleSubject is not null)
                {
                    logger.LogWarning(
                    "Google sign-in refused: user {UserId} is linked to a different Google account.",
                    user.Id);
                    return null;
                }

                var wasVerified = user.EmailVerified;

                user.GoogleSubject = google.Subject;

                if(!wasVerified)
                {
                    user.PasswordHash = null;
                    user.EmailVerified = true;
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                if (!wasVerified)
                {
                    await tokenService.RevokeAllAsync(user.Id, cancellationToken);
                }

                logger.LogInformation(
                   "User {UserId} linked to Google. Password kept: {PasswordKept}.",
                   user.Id,
                   wasVerified);


                return user;
            }

            // الحالة 3: مستخدم جديد = ننشئه
            user = new User
            {
                Email = email,
                UserName = BuildUserName(google),
                GoogleSubject = google.Subject,
                EmailVerified = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.Users.Add(user);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return user;
            } catch (DbUpdateException ex) when (attempt == 1 && IsUniqueConstraintViolation(ex))
            {
                // طلب تاني سبقنا وعمل نفس الحساب — نعيد المحاولة مرة ونلاقيه
                dbContext.Entry(user).State = EntityState.Detached;
            }
        }

    }


    private static string BuildUserName(GoogleUserInfo google)
    {
        var name = string.IsNullOrWhiteSpace(google.Name)
            ? google.Email.Split('@')[0]
            : google.Name;

        return name.Length <= 50
            ? name
            : name[..50];
    }
}
