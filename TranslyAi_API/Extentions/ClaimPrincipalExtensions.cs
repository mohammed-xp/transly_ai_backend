using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TranslyAi_API.Extentions
{
    public static class ClaimPrincipalExtensions
    {
        public static Guid? GetUserId(this ClaimsPrincipal principal)
        {
            var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(subject, out var userId) ? userId : null;
        }
    }
}
