using Microsoft.AspNetCore.Mvc;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Services.IServices;

namespace TranslyAI.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TonesController(IToneService toneService) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ApiResponse<ToneListResponseDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(ApiResponse<ToneListResponseDto>.Ok(await toneService.GetListedAsync(cancellationToken), "Tones received successfully"));
}
