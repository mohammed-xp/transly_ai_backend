using TranslyAI.Api.Dtos;

namespace TranslyAI.Api.Services.IServices;

public interface ITokenService
{
    Task<TokenDto> IssueAsync(Guid userId, CancellationToken cancellationToken);

    Task<TokenDto?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken);
}
