using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using System.Reflection.Metadata;
using System.Text;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Data;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Entities;
using TranslyAI.Api.Services.IServices;

namespace TranslyAI.Api.Services;

public class AuthService(
    TranslyDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IOptions<JwtOptions> jwtOption,
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
            throw new InvalidOperationException($"A user with the email '{registerRequest.Email}' already exists");
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
            throw new InvalidOperationException("An error occurred while registering the user", ex);
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
            var (token, expiresAt) = CreateAccessToken(user);
            var response = new LoginResponseDto
            {
                Token = new TokenDto { AccessToken = token, ExpiresAt = expiresAt },
                User = mapper.Map<UserDto>(user)
            };

            return response;
        }
        else if (result is PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, loginRequest.Password);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Password hash upgraded for user {UserId}.", user.Id);

            var (token, expiresAt) = CreateAccessToken(user);
            var response = new LoginResponseDto
            {
                Token = new TokenDto { AccessToken = token, ExpiresAt = expiresAt },
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

    private (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user)
    {
        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddMinutes(jwtOption.Value.AccessTokenMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOption.Value.SigningKey));
        SigningCredentials _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        JsonWebTokenHandler _handler = new();

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtOption.Value.Issuer,
            Audience = jwtOption.Value.Audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expiresAt,
            SigningCredentials = _signingCredentials,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString()
            }
        };

        return (_handler.CreateToken(descriptor), new DateTimeOffset(expiresAt, TimeSpan.Zero));
    }
}