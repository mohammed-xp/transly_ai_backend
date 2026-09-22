using TranslyAI.Api.Dtos;
using TranslyAI.Api.Entities;

namespace TranslyAI.Api.Services.IServices
{
    public interface IAuthService
    {
        Task<UserDto?> RegisterAsync(
        RegisterRequestDto registerRequest,
        CancellationToken cancellationToken);

        Task<bool> IsEmailExistsAsync(
        string email,
        CancellationToken cancellationToken);

        Task<LoginResponseDto?> LoginAsync(
        LoginRequestDto loginRequest,
        CancellationToken cancellationToken);

        Task<UserDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
    }
}
