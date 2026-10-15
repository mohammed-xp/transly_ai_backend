using System.ComponentModel.DataAnnotations;

namespace TranslyAI.Api.Entities;

public class User
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(254)]
    public required string Email { get; set; }

    [MaxLength(50)]
    public required string UserName { get; set; }

    [MaxLength(256)]
    public string? PasswordHash { get; set; }

    [MaxLength(255)]
    public string? GoogleSubject { get; set; }
    public bool EmailVerified { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}