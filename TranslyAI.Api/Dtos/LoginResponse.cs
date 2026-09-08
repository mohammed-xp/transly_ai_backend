namespace TranslyAI.Api.Dtos;

public record LoginResponse
{
    public required string AccessToken { get; init; }
    public required string TokenType { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required UserResponse User { get; init; }
}