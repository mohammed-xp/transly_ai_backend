using System.ComponentModel.DataAnnotations;
using TranslyAI.Api.Enums;

namespace TranslyAI.Api.AppSettings
{
    public sealed class PlanLimits
    {
        [Range(1, 10_000_000, ErrorMessage = "CharactersPerWindow must be between {1} and {2}.")]
        public required int CharactersPerWindow { get; init; }

        [EnumDataType(typeof(QuotaWindow), ErrorMessage = "Window is required and must be Daily or BillingPeriod.")]
        public required QuotaWindow Window { get; init; }

        [Range(1, 50_000, ErrorMessage = "MaxCharactersPerRequest must be between {1} and {2}.")]
        public required int MaxCharactersPerRequest { get; init; }
    }
}
