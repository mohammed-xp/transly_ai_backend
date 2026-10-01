
using System.ComponentModel.DataAnnotations;

namespace TranslyAI.Api.Dtos;

public class RefreshTokenRequestDto
{
    [StringLength(128, MinimumLength = 1)]
    public required string RefreshToken { get; init; }
}
