using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Dtos.Gemini;

namespace TranslyAI.Api.Tests.Dtos;

public class ConversationDtoTests
{
    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var request = NewRequest(history:
        [
            new() { Language = "ar", Text = "إزيك؟" },
            new() { Language = "en", Text = "I'm fine" },
        ]);

        var errors = ValidateModel(request);

        Assert.Empty(errors);
        Assert.Equal(2, request.History.Count);
    }

    [Fact]
    public void Validate_SameLanguages_ReturnsErrorOnLanguageB()
    {
        var request = NewRequest(languageA: "ar", languageB: "AR");

        var errors = ValidateModel(request);

        var error = Assert.Single(errors);
        Assert.Contains(nameof(ConversationTurnRequestDto.LanguageB), error.MemberNames);
    }

    [Fact]
    public void Validate_TooManyHistoryItems_ReturnsErrorOnHistory()
    {
        var items = Enumerable.Range(0, ConversationTurnRequestDto.MaxHistoryItems + 1)
            .Select(i => new ConversationHistoryItemDto { Language = "en", Text = $"message {i}" })
            .ToList();

        var request = NewRequest(history: items);

        var errors = ValidateModel(request);

        var error = Assert.Single(errors);
        Assert.Contains(nameof(ConversationTurnRequestDto.History), error.MemberNames);
    }

    [Fact]
    public void Validate_HistoryItemWithUnsupportedLanguage_ReturnsError()
    {
        var item = new ConversationHistoryItemDto { Language = "xx", Text = "hello" };

        var errors = ValidateModel(item);

        var error = Assert.Single(errors);
        Assert.Contains(nameof(ConversationHistoryItemDto.Language), error.MemberNames);
    }

    [Fact]
    public void GeminiAudioContent_SerializesToInteractionsShape()
    {
        var content = GeminiInputContent.FromAudio([1, 2, 3], "audio/wav");

        var json = JsonSerializer.Serialize(content);

        Assert.Equal("""{"type":"audio","data":"AQID","mime_type":"audio/wav"}""", json);
    }

    private static ConversationTurnRequestDto NewRequest(
        string languageA = "ar",
        string languageB = "en",
        List<ConversationHistoryItemDto>? history = null)
    {
        var bytes = Encoding.ASCII.GetBytes("fake audio bytes");
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "audio", "turn.wav");

        return new ConversationTurnRequestDto
        {
            Audio = file,
            LanguageA = languageA,
            LanguageB = languageB,
            History = history ?? [],
        };
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}