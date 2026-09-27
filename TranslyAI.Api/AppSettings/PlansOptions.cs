using System.ComponentModel.DataAnnotations;

namespace TranslyAI.Api.AppSettings;

public sealed class PlansOptions
{

    [Required(ErrorMessage = "DefaultPlan in appSettrings is Required.")]
    public required string DefaultPlan { get; init; }

    public required Dictionary<string, PlanLimits> Catalog { get; init; }
}


