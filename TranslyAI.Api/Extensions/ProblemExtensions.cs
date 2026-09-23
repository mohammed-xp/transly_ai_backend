using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace TranslyAI.Api.Extensions;

public static class ProblemExtensions
{
    /// <summary>
    /// Status 401 Unauthorized
    /// </summary>
    public static ObjectResult UnauthorizedProblem(this ControllerBase controller, string detail) =>
        controller.ProblemWithStatus(StatusCodes.Status401Unauthorized, detail);

    /// <summary>
    /// Status 409 Conflict
    /// </summary>
    public static ObjectResult ConflictProblem(this ControllerBase controller, string detail) =>
        controller.ProblemWithStatus(StatusCodes.Status409Conflict, detail);

    /// <summary>
    /// Status 429 Too Many Requests
    /// </summary>
    public static ObjectResult TooManyRequestsProblem(this ControllerBase controller, string detail) =>
        controller.ProblemWithStatus(StatusCodes.Status429TooManyRequests, detail);

    /// <summary>
    /// Status 500 Internal Server Error
    /// </summary>
    public static ObjectResult InternalErrorProblem(this ControllerBase controller, string detail) =>
        controller.ProblemWithStatus(StatusCodes.Status500InternalServerError, detail);

    /// <summary>
    /// Status 502 Bad Gateway
    /// </summary>
    public static ObjectResult BadGatewayProblem(this ControllerBase controller, string detail) =>
        controller.ProblemWithStatus(StatusCodes.Status502BadGateway, detail);

    /// <summary>
    /// Status 503 Service Unavailable
    /// </summary>
    public static ObjectResult ServiceUnavailableProblem(this ControllerBase controller, string detail) =>
        controller.ProblemWithStatus(StatusCodes.Status503ServiceUnavailable, detail);

    /// <summary>
    /// Status 504 Gateway Timeout
    /// </summary>
    public static ObjectResult GatewayTimeoutProblem(this ControllerBase controller, string detail) =>
        controller.ProblemWithStatus(StatusCodes.Status504GatewayTimeout, detail);

    // مكان واحد بيبني الـ ProblemDetails، والـ title بيتحسب من الـ status لوحده
    private static ObjectResult ProblemWithStatus(this ControllerBase controller, int statusCode, string detail) =>
        controller.Problem(
            detail: detail,
            statusCode: statusCode,
            title: ReasonPhrases.GetReasonPhrase(statusCode));
}