namespace TranslyAI.Api.Dtos
{
    public record TokenDto
    {
        public required string AccessToken { get; set; }
        //public required string RefreshToken { get; set; }
        public required DateTimeOffset ExpiresAt { get; set; }
    }
}
