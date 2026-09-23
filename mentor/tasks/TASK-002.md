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


---

## 📜 السجل (اتنقل من progress.md في 2026-09-23 — النص زي ما هو)

| TASK-001 | تنضيف الـ template + `/health` + أول commit | M0 | ✅ Done | Approved في الـ round التالت — اتنين rounds اتضاعوا في acceptance criteria متقروش |

| TASK-002 | `POST /v1/translations` بـ records و fake logic | M2 | ✅ Done | Approved في r3. الـ contract مطابق حرف بحرف، `required`+`init` على الكل، 400 ProblemDetails مجاناً. الـ rounds الزيادة كانت ملاحظات متطبقتش مش أخطاء كود |

## المشروع

- **التطبيق:** Transly AI — تطبيق Flutter لترجمة النصوص بالـ AI
- **الـ slice المتفق عليه:** الترجمة end-to-end من خلال الباك اند — التطبيق يبعت النص واللغة للـ API، والـ API هو اللي بيكلم Gemini ويرجع الترجمة. الـ Gemini key يقعد على السيرفر بس.
- **باك اند موجود قبل كده؟** لأ.
- **تصحيح مهم (2026-08-15):** التطبيق **مش** بينادي Gemini ولا أي AI. الـ `AiTranslationStubDatasource` بيرجّع `'[AI stub] $text'` بعد delay. الترجمة الحقيقية الشغالة = **ML Kit on-device**، والـ `TranslationRepositoryImpl` بيختار بينهم بالـ connectivity. يعني **مفيش key في التطبيق يتسرب** — الباك اند بيملا فراغ، مش بيصلح خطر قايم. الدافع الحقيقي = الاشتراكات + الـ quota.
- **✅ اتحسم (2026-08-16): المزوّد = Gemini API** (Google AI Studio). الـ key جاهز عنده. الـ `api_endpoints.dart` بيقول Claude — ده placeholder قديم في كود الفلاتر، مش قرار.
- **ريبو الفلاتر:** `mohammed-xp/transly_ai` (private, branch `main`). الوصول عن طريق `gh` — متسطب في `C:\Program Files\GitHub CLI\gh.exe` (مش على الـ PATH بتاع الـ shell) ومسجّل دخول بحسابه بـ scope `repo`.

## الـ Contract المستخرج من تطبيق الفلاتر

```dart
TranslationRequest { String text; LanguagePair pair; TranslationTone tone = formal; }
Translation { sourceText, translatedText, pair, tone, engine = mlKit, createdAt? }
LanguagePair { Language source; Language target; }
Language { code, name, nativeName, isRtl = false, isOfflineAvailable = false }
enum TranslationTone { formal, casual, short }
enum TranslationEngine { mlKit, ai }
```

- `ApiEndpoints.baseUrl = 'https://api.transly.ai/v1'` · `/translate` · `/languages`
- الـ error contract في الـ client: `sealed class Failure` (Network/Server/Cache/Offline/Unknown/Permission) — يتقابل مع `ProblemDetails` في M4
- فيه feature `history` كاملة شغالة على `history_local_datasource` — هتتنقل للسيرفر في M3/M5. `HistoryEntry` فيها `id` و `isFavorite`.
- `supportedLanguages()` دلوقتي بيرجّع `Language.wellKnown` hardcoded — مرشح لـ `GET /v1/languages`

- 🔴 **`DateTime` بدل `DateTimeOffset` — 3 مرات** (TASK-001 r1، TASK-002 r1، وبعد ما اتنبّه في رسالة الـ unblock). القرار #4.

## أسئلة intake لسه مجاوبش عليها

1. ✅ ~~فين ريبو الفلاتر؟~~ — `mohammed-xp/transly_ai`، اتقرا والـ contract اتستخرج
2. مستوى الـ C#: LINQ و EF Core migrations عملهم بإيده قبل كده ولا لأ؟
3. ✅ ~~التطبيق فيه users/Firebase Auth دلوقتي؟~~ — **اتقفل 2026-08-30 بإجابة منه: مفيش أي auth خالص.** (اتسأل بالكلام مش بقراية كود الفلاتر — القاعدة #1). النتيجة: M5 تصميم من الصفر، وإحنا اللي بنصدر الـ JWT
4. ✅ ~~الوقت الأسبوعي بالساعات~~ — **اتقفل بقرار منه 2026-09-02، والسؤال ممنوع يتسأل تاني.** نصه: **«مالكش دعوة انا وقتي المتاح كم، اعطني التاسك وشوف الحجم المناسب وانا اشتغل عليها سواء خلصتها في نفس اليوم او بكرة او بعد اسبوع ما مهم، المهم اخلصها وافهمها.»**
   - **الأثر على كتابة أي ticket جاي:** التاسك **تتحجّم بالمحتوى** (كام جزء، وكل جزء بيعلّم إيه) مش بالوقت المتاح. **وأبواب الخروج المعلنة** (اللي اتحطت في TASK-015 و016 عشان ما أخمّنش وقته) **مبقاش ليها سبب** — بدلها: كل جزء يتقفل على نفسه بحيث ينفع يسيبها ويرجعلها.
   - **وده بيسحب أهم عذر كنت بعلّقه على الحجم.** التاسك تبقى بحجمها الصح، والسرعة مش معياري.
5. ✅ ~~Gemini ولا Claude؟~~ — **Gemini**، والـ key جاهز عنده (2026-08-16)

