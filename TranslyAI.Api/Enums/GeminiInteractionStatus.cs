namespace TranslyAI.Api.Enums;

public enum GeminiInteractionStatus
{
    Unknown = 0,
    Queued,
    InProgress,
    RequiresAction,
    Completed,
    Failed,
    Cancelled,
    Incomplete,
    BudgetExceeded,
}
