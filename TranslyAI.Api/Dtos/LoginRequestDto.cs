using System.ComponentModel.DataAnnotations;

namespace TranslyAI.Api.Dtos;

public record LoginRequestDto
{
    [StringLength(254, MinimumLength = 1)]
    public required string Email { get; init; }

    [StringLength(256, MinimumLength = 1)]
    public required string Password { get; init; }
}