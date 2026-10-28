namespace TranslyAI.Api.Dtos
{
    public record UsageResponseDto
    {
        public required string Plan { get; init; }
        public required int CharactersLimit { get; init; }
        public required int CharactersUsed { get; init; }
        public required int CharactersRemaining { get; init; }
        public required int AudioSecondsLimit { get; init; }
        public required int AudioSecondsUsed { get; init; }
        public required int AudioSecondsRemaining { get; init; }
        public required DateTimeOffset ResetsAt { get; init; }
    }
}
