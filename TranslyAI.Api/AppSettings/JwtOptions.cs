using System.ComponentModel.DataAnnotations;

namespace TranslyAI.Api.AppSettings;

public class JwtOptions
{
    [Required(ErrorMessage = "Jwt Issuer is required.")]
    public required string Issuer { get; init; }
    [Required(ErrorMessage = "Jwt Audience is required.")]
    public required string Audience { get; init; }

    [Required(ErrorMessage = "Jwt SigningKey is required.")]
    [MinLength(32, ErrorMessage = "Jwt SigningKey must be at least 32 characters (256 bits) for HS256.")]
    public required string SigningKey { get; init; }

    [Range(1, 1440, ErrorMessage = "Jwt AccessTokenMinutes must be between 1 and 1440.")]
    public required int AccessTokenMinutes { get; init; }
}