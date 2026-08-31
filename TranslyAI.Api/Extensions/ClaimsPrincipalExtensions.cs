using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TranslyAI.Api.Extensions;

public static class ClaimPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(subject, out var userId) ? userId : null;
    }
}