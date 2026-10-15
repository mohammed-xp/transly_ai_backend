using System.ComponentModel.DataAnnotations;

namespace TranslyAI.Api.Dtos;

public record DeleteAccountRequestDto
{
    [StringLength(256, MinimumLength = 1)]
    public string? Password { get; init; }

    [StringLength(4096, MinimumLength = 1)]
    public string? IdToken { get; init; }
}
