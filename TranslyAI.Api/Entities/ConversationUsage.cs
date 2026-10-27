using System.ComponentModel.DataAnnotations;
using TranslyAI.Api.Enums;

namespace TranslyAI.Api.Entities;

public class ConversationUsage
{
    public long Id { get; set; }
    
    public required Guid UserId { get; set; }

    [MaxLength(8)]
    public required string LanguageA { get; set; }

    [MaxLength(8)]
    public required string LanguageB { get; set; }

    public required int AudioSeconds { get; set; }

    public required UsageStatus Status { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
