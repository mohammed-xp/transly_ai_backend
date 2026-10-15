using System.ComponentModel.DataAnnotations;

namespace TranslyAI.Api.Dtos
{
    public record GoogleLoginRequestDto
    {
        [StringLength(4096, MinimumLength = 1)]
        public required string IdToken { get; init; }
    }
}
