namespace TranslyAI.Api.Dtos;

public record RegisterResponse
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string UserName { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}