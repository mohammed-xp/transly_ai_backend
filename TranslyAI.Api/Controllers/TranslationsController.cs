using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("v1/[controller]")]
public class TranslationsController : ControllerBase
{
    [HttpPost]
    public IActionResult Translations([FromBody] TranslationRequest translation)
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
            CreatedAt = DateTime.UtcNow
        };
        return Ok(translationResponse);
    }
}