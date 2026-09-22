using System.ComponentModel.DataAnnotations;

namespace TranslyAI.Api.Dtos;

public record RegisterRequestDto
{
    [EmailAddress]
    [StringLength(254, MinimumLength = 6)]
    public required string Email { get; init; }

    [StringLength(50, MinimumLength = 3)]
    public required string UserName { get; init; }

    [StringLength(128, MinimumLength = 8)]
    public required string Password { get; init; }
}