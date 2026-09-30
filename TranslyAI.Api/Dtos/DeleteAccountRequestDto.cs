using System.ComponentModel.DataAnnotations;

namespace TranslyAI.Api.Dtos;

public record DeleteAccountRequestDto
{
    [StringLength(256, MinimumLength = 1)]
    public required string Password { get; init; }
}
