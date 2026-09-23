using TranslyAI.Api.Entities;

namespace TranslyAI.Api.Dtos;

public record UserDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string UserName { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}