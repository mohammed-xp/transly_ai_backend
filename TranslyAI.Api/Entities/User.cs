using System.ComponentModel.DataAnnotations;

namespace TranslyAI.Api.Entities;

public class User
{
    public Guid Id { get; set; }
    [MaxLength(254)]
    public required string Email { get; set; }
    [MaxLength(50)]
    public required string UserName { get; set; }
    [MaxLength(256)]
    public required string PasswordHash { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
}