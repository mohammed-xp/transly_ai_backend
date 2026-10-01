using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Data;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Entities;
using TranslyAI.Api.Services.IServices;

namespace TranslyAI.Api.Services;

public class TokenService(
    TranslyDbContext dbContext,
    IOptions<JwtOptions> jwtOptions,
    ILogger<TokenService> logger) : ITokenService
{
    public async Task<TokenDto> IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var (refreshToken, entity) = CreateRefreshToken(userId, Guid.CreateVersion7(), now);

        dbContext.RefreshTokens.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreateTokenDto(userId, refreshToken, now);
    }

    public async Task<TokenDto?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var hash = Hash(refreshToken);

        var stored = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null)
        {
            return null;
        }

        if (stored.RevokedAtUtc is not null)
        {
            await RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            logger.LogWarning(
                "Refresh token reuse detected for user {UserId}. Session {FamilyId} revoked.",
                stored.UserId,
                stored.FamilyId);

            return null;
        }

        if (stored.ExpiresAtUtc <= now)
        {
            return null;
        }

        stored.RevokedAtUtc = now;

        var (newRefreshToken, replacement) = CreateRefreshToken(stored.UserId, stored.FamilyId, now);
        dbContext.RefreshTokens.Add(replacement);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }

        return CreateTokenDto(stored.UserId, newRefreshToken, now);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = Hash(refreshToken);

        var familyId = await dbContext.RefreshTokens
            .Where(t => t.TokenHash == hash)
            .Select(t => (Guid?)t.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (familyId is null)
        {
            return;
        }

        await RevokeFamilyAsync(familyId.Value, DateTime.UtcNow, cancellationToken);
    }

    private Task<int> RevokeFamilyAsync(Guid familyId, DateTime now, CancellationToken cancellationToken)
        => dbContext.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, now), cancellationToken);

    private (string Token, RefreshToken Entity) CreateRefreshToken(Guid userId, Guid familyId, DateTime now)
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

        var entity = new RefreshToken
        {
            UserId = userId,
            FamilyId = familyId,
            TokenHash = Hash(token),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(jwtOptions.Value.RefreshTokenDays)
        };

        return (token, entity);
    }

    private TokenDto CreateTokenDto(Guid userId, string refreshToken, DateTime now)
    {
        var options = jwtOptions.Value;
        var expiresAt = now.AddMinutes(options.AccessTokenMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userId.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString()
            }
        };

        return new TokenDto
        {
            AccessToken = new JsonWebTokenHandler().CreateToken(descriptor),
            RefreshToken = refreshToken,
            ExpiresAt = new DateTimeOffset(expiresAt, TimeSpan.Zero)
        };
    }

    private static string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
