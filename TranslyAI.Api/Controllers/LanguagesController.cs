using Microsoft.AspNetCore.Mvc;
using TranslyAI.Api.Common;
using TranslyAI.Api.Dtos;

namespace TranslyAI.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class LanguagesController : ControllerBase
{

    [HttpGet]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public ActionResult<LanguageResponse> GetAll() =>
        Ok(new LanguageResponse { Languages = LanguageCatalog.All });
}