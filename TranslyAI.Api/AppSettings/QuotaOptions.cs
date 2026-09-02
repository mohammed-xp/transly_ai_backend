using System.ComponentModel.DataAnnotations;

namespace TranslyAI.Api.AppSettings;

public class QuotaOptions
{
    [Range(1, 100_000, ErrorMessage = "Quota RequestsPerDay must be between 1 and 100000.")]
    public required int RequestsPerDay { get; init; }
}