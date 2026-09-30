using TranslyAI.Api.Dtos;
using TranslyAI.Api.Entities;
using TranslyAI.Api.Enums;

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

        Task<DeleteAccountResult> DeleteAccountAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken);
    }
}
