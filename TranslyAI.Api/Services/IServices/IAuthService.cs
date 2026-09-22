using TranslyAI.Api.Dtos;

namespace TranslyAI.Api.Services.IServices
{
    public interface IAuthService
    {
        Task<UserDto?> RegisterAsync(
        RegisterRequest registerRequest,
        CancellationToken cancellationToken);
    }
}
