using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using TranslyAI.Api.AppSettings;
using TranslyAI.Api.Services.IServices;
using TranslyAI.Api.Services.Result;
using static Google.Apis.Auth.GoogleJsonWebSignature;

namespace TranslyAI.Api.Services
{
    public class GoogleTokenValidator(
        IOptions<GoogleAuthOptions> options,
        ILogger<GoogleTokenValidator> logger) : IGoogleTokenValidator
    {
        public async Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken)
        {
            Payload payload;

            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    idToken,
                    new ValidationSettings
                    {
                        Audience = options.Value.ClientIds
                    });
            }
            catch (Exception ex) when (ex is InvalidJwtException or FormatException or Newtonsoft.Json.JsonException)
            {
                logger.LogInformation("Google ID token rejected: {Reason}", ex.Message);
                return null;
            }

            if(!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
            {
                logger.LogInformation("Google ID token rejected: Email not verified or missing");
                return null;
            }

            return new GoogleUserInfo(payload.Subject, payload.Email, payload.Name);
        }
    }
}
