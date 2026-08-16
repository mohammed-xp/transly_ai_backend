using Microsoft.AspNetCore.Mvc;
using TranslyAI.Api.Dtos;

[ApiController]
[Route("v1/[controller]")]
public class TranslationsController : ControllerBase
{
    [HttpPost]
    public IActionResult Translate(TranslationRequest translation)
    {
        char[] chars = translation.Text.ToCharArray();
        Array.Reverse(chars);
        string translatedText = new string(chars);

        var translationResponse = new TranslationResponse
        {
            SourceText = translation.Text,
            TranslatedText = translatedText,
            SourceLanguage = translation.SourceLanguage,
            TargetLanguage = translation.TargetLanguage,
            Tone = translation.Tone,
            Model = "stub",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        return Ok(translationResponse);
    }
}