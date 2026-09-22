namespace TranslyAI.Api.Dtos;

public record LoginResponseDto
{
    public required TokenDto Token { get; init; }
    public required UserDto User { get; init; }
}