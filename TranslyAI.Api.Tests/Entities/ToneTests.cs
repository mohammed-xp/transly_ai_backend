using TranslyAI.Api.Common;
using TranslyAI.Api.Entities;

namespace TranslyAI.Api.Tests.Entities;

public class ToneTests
{
    [Fact]
    public void Constructor_AddsDefaultCultureTranslation()
    {
        var tone = NewTone();

        var translation = Assert.Single(tone.Translations);
        Assert.Equal(AppCultures.Default, translation.Culture);
        Assert.Equal("Formal", translation.Name);
        Assert.Equal("Polished.", translation.Description);
    }

    [Fact]
    public void SetTranslation_ForNewCulture_AddsIt()
    {
        var tone = NewTone();

        tone.SetTranslation("ar", "رسمي", "مصقول.");

        var arabic = Assert.Single(tone.Translations, t => t.Culture == "ar");
        Assert.Equal("رسمي", arabic.Name);
        Assert.Equal(2, tone.Translations.Count);
    }

    [Fact]
    public void SetTranslation_ForExistingCulture_UpdatesIt()
    {
        var tone = NewTone();

        tone.SetTranslation(AppCultures.Default, "Professional", "Updated.");

        var translation = Assert.Single(tone.Translations);
        Assert.Equal("Professional", translation.Name);
        Assert.Equal("Updated.", translation.Description);
    }

    [Fact]
    public void RemoveTranslation_ForDefaultCulture_Throws()
    {
        var tone = NewTone();

        Assert.Throws<InvalidOperationException>(() => tone.RemoveTranslation(AppCultures.Default));
        Assert.Single(tone.Translations);
    }

    [Fact]
    public void RemoveTranslation_ForOtherCulture_RemovesIt()
    {
        var tone = NewTone();
        tone.SetTranslation("ar", "رسمي", "مصقول.");

        tone.RemoveTranslation("ar");

        Assert.DoesNotContain(tone.Translations, t => t.Culture == "ar");
    }

    private static Tone NewTone() => new("formal", "Formal", "Polished.", "Use a formal register.");
}
