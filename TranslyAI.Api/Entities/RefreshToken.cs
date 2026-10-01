
using System.ComponentModel.DataAnnotations;

namespace TranslyAI.Api.Entities;

public class RefreshToken
{
    public long Id { get; set; }
    public required Guid UserId { get; set; }
    public required Guid FamilyId { get; set; }
    [MaxLength(64)]
    public required string TokenHash { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
    public required DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
