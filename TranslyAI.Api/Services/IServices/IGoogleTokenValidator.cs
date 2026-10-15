using TranslyAI.Api.Services.Result;

namespace TranslyAI.Api.Services.IServices
{
    public interface IGoogleTokenValidator
    {
        Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken);
    }
}
