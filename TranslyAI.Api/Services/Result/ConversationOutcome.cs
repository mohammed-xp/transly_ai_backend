namespace TranslyAI.Api.Services.Result;

public abstract record ConversationOutcome
{
    private ConversationOutcome() { }

    // ── Gemini ردّ بنتيجة مفهومة ─────────────────────────────

    // فهم الكلام وترجم
    public sealed record Translated(
        string SourceLanguage,
        string TargetLanguage,
        string Transcript,
        string Translation,
        string Model,
        DateTimeOffset CreatedAt
    ) : ConversationOutcome;

    // مفيش كلام واضح (سكوت / دوشة / تسجيل قصير جداً)
    public sealed record NoSpeech(
        string Model
    ) : ConversationOutcome;

    // الكلام بلغة غير اللغتين اللي المستخدم اختارهم
    public sealed record UnsupportedLanguage(
        string Model
    ) : ConversationOutcome;

    // ── رفض قبل ما نكلم Gemini ──────────────────────────────

    // الملف مش WAV سليم أو الـ format مش مدعوم
    public sealed record InvalidAudio(
        string Reason
    ) : ConversationOutcome;

    // التسجيل أطول من المسموح في الـ plan
    public sealed record AudioTooLong(
        int MaxSeconds
    ) : ConversationOutcome;

    // الثواني المتبقية في الـ quota مش كفاية
    public sealed record QuotaExceeded() : ConversationOutcome;

    // ── Gemini أو الشبكة فشلوا ──────────────────────────────

    // بنعيد استخدام أخطاء الترجمة العادية عشان الـ controller يحولها لنفس الـ status codes.
    // القيم المتوقعة هنا بس: RateLimited, InvalidRequest, UpstreamError, UpstreamTimeout, NotCompleted
    public sealed record UpstreamFailure(
        TranslationOutcome Error
    ) : ConversationOutcome;
}