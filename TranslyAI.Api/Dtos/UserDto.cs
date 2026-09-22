using TranslyAI.Api.Entities;

namespace TranslyAI.Api.Dtos;

public record UserDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string UserName { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public static UserDto From(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        UserName = user.UserName,
        CreatedAt = new DateTimeOffset(user.CreatedAtUtc, TimeSpan.Zero)
    };
}