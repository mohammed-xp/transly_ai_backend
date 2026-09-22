using Microsoft.AspNetCore.Mvc;
using TranslyAI.Api.Common;
using TranslyAI.Api.Dtos;

namespace TranslyAI.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class LanguagesController : ControllerBase
{

    [HttpGet]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public ActionResult<ApiResponse<LanguageResponseDto>> GetAll() =>
        Ok(ApiResponse<LanguageResponseDto>.Ok(new LanguageResponseDto { Languages = LanguageCatalog.All }, "Language received successfully"));
}
