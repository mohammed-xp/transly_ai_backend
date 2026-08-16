# 🎫 TASK-002 — عقد الترجمة (من غير AI)

**Milestone:** M2 — First real endpoints · **الوقت المتوقع:** ~90 دقيقة · **الصعوبة:** ▓▓░░░
**الحالة:** 🚧 In progress · **اتفتحت:** 2026-08-15

## الهدف

`POST /v1/translations` يستقبل نص ولغتين ونبرة، ويرجّع ترجمة **مزيفة** بالشكل النهائي بالظبط.
بعد التاسك دي تقدر توجّه تطبيق الفلاتر على الباك اند وتشوفه شغال — بترجمة غلط، بـ contract صح.

## ليه دلوقتي

أصعب حاجة في أي API هي شكل الـ request والـ response، مش المنطق اللي بينهم.
لو ظبطنا الشكل الأول ووصّلنا التطبيق عليه، إضافة الـ AI بعد كده بتبقى تغيير جوه method واحدة والـ Flutter ميحسش بحاجة.
العكس — تكتب الـ AI integration الأول وتكتشف إن الـ shape غلط — يعني تعدّل في الاتنين.

## الـ Contract

### Request

```jsonc
POST /v1/translations
Content-Type: application/json

{
  "text": "Good morning",
  "sourceLanguage": "en",     // ISO 639-1, required
  "targetLanguage": "ar",     // ISO 639-1, required
  "tone": "formal"            // optional → formal | casual | short
}
```

### Response

```jsonc
200 OK

{
  "sourceText": "Good morning",
  "translatedText": "[fake] Good morning",
  "sourceLanguage": "en",
  "targetLanguage": "ar",
  "tone": "formal",
  "model": "stub",
  "createdAt": "2026-08-15T11:22:08+00:00"
}
```

## المطلوب

- [ ] `TranslationsController` — أول controller حقيقي، بـ `[ApiController]`
- [ ] الـ route: `POST /v1/translations`
- [ ] request DTO و response DTO — كل واحد `record` في ملف منفصل
- [ ] `tone` optional، الافتراضي `formal`
- [ ] `sourceLanguage` و `targetLanguage` **required** (مفيش auto-detect)
- [ ] المنطق مزيف — رجّع النص مقلوب أو `[fake] {text}`. **مفيش `HttpClient` ولا Gemini/Claude خالص**
- [ ] status code صح — 200 مش 201 (السبب في القرار #7)
- [ ] `dotnet format` قبل الـ commit

## خارج الـ scope

Gemini/Claude · `HttpClient` · أي validation على الـ input · database · حفظ الـ history · auth · error handling · `GET /v1/languages` · `id` في الـ response · quota headers

لو النص فاضي أو اللغة غلط — مش مشكلتك دلوقتي.

## مفاتيح تدور بيها

- `record` — بالذات `init` و `required`
- `[ApiController]` · `[HttpPost]` · `[Route]`
- `[FromBody]` و model binding
- `IActionResult` vs `ActionResult<T>` vs `Results` — الفرق بينهم
- ازاي الـ enum بيتعمله serialize كـ string مش رقم في `System.Text.Json`
- [Create web APIs with ASP.NET Core](https://learn.microsoft.com/aspnet/core/web-api/)

## Definition of Done

`POST http://localhost:5260/v1/translations` من Postman بالـ JSON body اللي فوق → **200** بالـ response shape بالظبط.
وابعت screenshot الـ request والـ response.

---

## القرارات المرتبطة

- **#6** — `record` مش `class` للـ DTOs
- **#7** — `/v1/translations` بـ 200
- **#8** — DTO مسطّح، مش نسخة من `LanguagePair`
- **#9** — `model` string مش `engine` enum
- **#10** — مفيش `id` لسه
- **#11** — مفيش envelope

التفاصيل في [progress.md](../progress.md).
