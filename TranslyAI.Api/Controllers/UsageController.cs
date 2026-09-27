using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Extensions;
using TranslyAI.Api.Services.IServices;

namespace TranslyAI.Api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class UsageController(IQuotaService quotaService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<ApiResponse<UsageResponseDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<UsageResponseDto>>> Get(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();

            if(userId is null)
            {
                return this.UnauthorizedProblem("The access token is invalid");
            }

            var quota = await quotaService.GetSnapshotAsync(userId.Value, cancellationToken);

            var usage = new UsageResponseDto
            {
                Plan = quota.Plan,
                CharactersLimit = quota.Limit,
                CharactersRemaining = quota.Remaining,
                CharactersUsed = quota.Used,
                ResetsAt = quota.ResetsAt
            };

            return Ok(ApiResponse<UsageResponseDto>.Ok(usage, "Usage retrieved successfully"));
        }
    }
}
