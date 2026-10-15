using System.ComponentModel.DataAnnotations;

namespace TranslyAI.Api.AppSettings
{
    public class GoogleAuthOptions
    {
        [Required(ErrorMessage = "Google ClientIds is required.")]
        [MinLength(1, ErrorMessage = "Google ClientIds must contain at least one OAuth client ID.")]
        public required string[] ClientIds { get; init; }
    }
}
