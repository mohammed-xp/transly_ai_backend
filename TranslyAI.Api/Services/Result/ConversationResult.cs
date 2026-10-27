namespace TranslyAI.Api.Services.Result;

public sealed record ConversationResult(
    ConversationOutcome Outcome,
    QuotaSnapshot Quota,
    int AudioSeconds
);