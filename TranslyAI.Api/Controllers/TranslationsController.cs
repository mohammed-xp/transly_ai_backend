using Microsoft.AspNetCore.Mvc;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Services;

namespace TranslyAI.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class TranslationsController(GeminiApiService geminiApiService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Translate(TranslationRequest translation, CancellationToken cancellationToken)
    {
        var response = await geminiApiService.TranslateAsync(translation, cancellationToken);

        if (response == null)
        {
            return StatusCode(502, "Failed to get a response from the Gemini API.");
        }

        var tResponse = new TranslationResponse
        {
            SourceText = translation.Text,
            TranslatedText = response.Text,
            SourceLanguage = translation.SourceLanguage,
            TargetLanguage = translation.TargetLanguage,
            Model = response.ModelVersion,
            Tone = translation.Tone,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        return Ok(tResponse);
    }
}