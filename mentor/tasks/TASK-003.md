# 🎫 TASK-003 — الـ AI proxy الحقيقي: تكلّم Gemini من السيرفر

**Milestone:** M2 / AI proxy · **الوقت المتوقع:** ~120 دقيقة · **الصعوبة:** ▓▓▓▓░
**اتفتحت:** 2026-08-16 · **اتقفلت:** 2026-08-19 · **الحالة:** ✅ Approved في r4

> **متبقي منها (🟡 من الـ review، اتنقلت لتاسك تقوية):** `finishReason` في الـ log · `ValidateOnStart()` للـ `ApiKey` · فصل `ModelVersion` الفاضية عن الفشل · `HttpClient.Timeout` · تثبيت اسم موديل صريح بدل الـ alias.

---

## الهدف

`POST /v1/translations` يرجّع **ترجمة حقيقية** من Gemini بدل عكس النص. التطبيق يبعت النص + اللغتين + النبرة، السيرفر يبني الـ prompt، ينادي Gemini، ويرجّع النص المترجم في نفس الـ contract اللي اتفقنا عليه في TASK-002 — من غير ما يتغير حرف واحد في شكل الـ response.

الـ API key يقعد على السيرفر بس، ومش بيتكوميت.

## ليه دلوقتي

عندك endpoint بـ contract مظبوط ومنطق فاضي. التاسك دي هي اللي بتحوّل المشروع من تمرين لحاجة حقيقية — ومعاها بتاخد تلات حاجات أساسية في .NET مع بعض في سياق واحد: `IHttpClientFactory` والـ typed clients، الـ Options pattern للـ configuration، و user secrets.

دي كمان أول مرة تنادي فيها خدمة خارجية من ASP.NET Core — يعني أول `async` حقيقي، وأول `CancellationToken` ليه معنى.

## المطلوب

- [ ] class اسمه `GeminiOptions` (أو أي اسم واضح) بيتربط بـ section اسمه `Gemini` في الـ configuration، وفيه على الأقل: `ApiKey`, `Model`
- [ ] الـ **key في user secrets**، مش في `appsettings.json` ولا `appsettings.Development.json`. `Model` عادي يقعد في `appsettings.json`
- [ ] **typed HttpClient** متسجّل في الـ DI عن طريق `AddHttpClient<T>` — مش `new HttpClient()` ولا `static HttpClient`
- [ ] الـ class ده بياخد النص واللغتين والنبرة، يبني الـ prompt، ينادي Gemini، ويرجّع النص المترجم
- [ ] الـ action في الـ controller بقى `async Task<IActionResult>` وبياخد `CancellationToken` وبيمرّرها لحد الـ HTTP call
- [ ] النبرة **فعلاً بتغيّر الناتج** — `formal` / `casual` / `short` يدّوا نتايج مختلفة على نفس النص
- [ ] `model` في الـ response = اسم الموديل الحقيقي من الـ configuration (مثلاً `gemini-2.5-flash`)، مش `"stub"`
- [ ] الـ `translatedText` فيه **النص المترجم بس** — من غير `"Here is the translation:"` ولا quotes زيادة ولا شرح
- [ ] لو Gemini وقع أو عمل timeout → الـ endpoint يرجّع **502**، من غير stack trace ومن غير أي أثر للـ key في الـ response
- [ ] nit من TASK-002 لسه واقف: `TranslationsController.cs` من غير `namespace` — ضيفه وانت بتعدّل الملف

## خارج الـ scope

مش دلوقتي — كل واحدة فيهم ليها وقتها:

- ❌ Retries / Polly / circuit breaker → M6
- ❌ Streaming (SSE) → بعد ما الأساس يشتغل
- ❌ Caching للترجمات المتكررة → M6
- ❌ حفظ الترجمة في database → M3
- ❌ `TranslationTone` كـ enum بدل string → تاسك لوحدها
- ❌ `GET /v1/languages`
- ❌ Unit tests / interfaces عشان الـ tests → M7
- ❌ Exception handling middleware عامة (`IExceptionHandler`) → M4. دلوقتي `try/catch` محلي في مكان واحد كفاية
- ❌ NuGet package للـ Gemini SDK — اقرا الـ decision block رقم 1

## مفاتيح تدور بيها

**.NET:**
- `IHttpClientFactory` typed clients → https://learn.microsoft.com/aspnet/core/fundamentals/http-requests
- Options pattern (`IOptions<T>` vs `IOptionsSnapshot<T>` vs `IOptionsMonitor<T>`) → https://learn.microsoft.com/aspnet/core/fundamentals/configuration/options
- `dotnet user-secrets init` / `dotnet user-secrets set` → https://learn.microsoft.com/aspnet/core/security/app-secrets
- `System.Net.Http.Json` → `PostAsJsonAsync`, `ReadFromJsonAsync`
- `HttpClient.Timeout` و `CancellationToken` في الـ minimal/controller actions

**Gemini:**
- `generateContent` REST reference → https://ai.google.dev/api/generate-content
- Text generation guide → https://ai.google.dev/gemini-api/docs/text-generation
- الـ endpoint: `POST https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent`
- الـ auth: header اسمه `x-goog-api-key` (مش Bearer)
- دوّر على `systemInstruction` في الـ request body — هي المكان الصح لتعليمات "ترجم بس ومترغيش"

## Definition of Done

السيرفر شغال بـ `dotnet run`، والطلب ده:

```
POST http://localhost:5xxx/v1/translations
{
  "text": "صباح الخير يا صاحبي، عامل ايه النهاردة؟",
  "sourceLanguage": "ar",
  "targetLanguage": "en",
  "tone": "casual"
}
```

يرجّع 200 وفيه `translatedText` بترجمة إنجليزي حقيقية بنبرة عامية، و`model` باسم الموديل.

وبعدين **نفس الطلب بالظبط** بـ `"tone": "formal"` يدّي صياغة مختلفة وأرسم.

وأخيراً: `git status` نضيف من أي key، و`dotnet format` عدّى.

---

## 🧭 قرار معماري 1: ازاي ننادي HTTP من ASP.NET Core

- **اخترنا:** `IHttpClientFactory` + typed client (`builder.Services.AddHttpClient<GeminiTranslationService>(...)`)، بـ `HttpClient` خام من غير SDK
- **البدائل:** `new HttpClient()` جوه الـ method · `static readonly HttpClient` · NuGet SDK لـ Gemini
- **ليه الـ factory:** `new HttpClient()` في كل request = socket exhaustion. الـ socket بيفضل في `TIME_WAIT` بعد الـ `Dispose`، وتحت ضغط السيرفر بيقع بـ `SocketException`. الـ `static HttpClient` بيحل ده بس بيخلق مشكلة تانية: بيكاش الـ DNS للأبد، فلو المزوّد غيّر الـ IP بتفضل تكلم عنوان ميت. الـ factory بيدير pool من الـ handlers وبيعمل rotate ليهم كل دقيقتين — بيحل الاتنين.
- **ليه من غير SDK:** الهدف هنا إنك تتعلم الـ HTTP layer في .NET، مش تتعلم API لـ wrapper. وكمان `generateContent` request بسيط جداً — الـ SDK هيضيف dependency وتحديثات ومفاهيم مقابل توفير ٢٠ سطر.
- **إمتى كنت هتاخد SDK:** لو كنت هتستعمل streaming، function calling، file uploads، وcount tokens — ساعتها الـ wrapper بيوفر شغل حقيقي مش ٢٠ سطر.
- **الغلطة الشائعة:** الناس بتعمل `AddHttpClient` وبعدين تعمل `new HttpClient()` جوه الـ class برضه من غير ما تاخد بالها. لو الـ class بتاعك مش بياخد `HttpClient` في الـ constructor، يبقى الـ factory مش شغالة.

## 🧭 قرار معماري 2: الـ API key بيقعد فين

- **اخترنا:** **user secrets** في الـ development، و environment variables في الـ production (M8)
- **البدائل:** `appsettings.Development.json` مع `.gitignore` · env var من دلوقتي · Azure Key Vault
- **ليه:** user secrets بيتحط في `%APPDATA%\Microsoft\UserSecrets\` — **بره الريبو خالص**. يعني مستحيل تكوميته بالغلط، ومستحيل يتسرب لو شاركت المجلد. و`IConfiguration` بيقراه تلقائي في الـ Development من غير أي كود زيادة، فالكود مبيعرفش أصلاً الـ key جه منين.
- **ليه مش `appsettings.Development.json` + gitignore:** أي حد بيعمل clone وبيشتغل هيقع في `git add -A` مرة. وأخطر: الملف ده أصلاً **متكوميت دلوقتي** في الريبو بتاعك — يعني هتحتاج تشيله من الـ history لو حطيت فيه key. الحماية اللي بتعتمد على انضباطك مش حماية.
- **ليه مش Key Vault دلوقتي:** طبقة infra كاملة عشان مشروع لسه مش منشور. لما ننشر على Azure في M8 هنتكلم فيها.
- **الغلطة الشائعة:** `dotnet user-secrets` بيشتغل بس لما يكون فيه `UserSecretsId` في الـ `.csproj` — الـ `init` بيضيفه. ولو شغّلت الأمر من فولدر غلط، بتلاقي الـ key اتحط في مشروع تاني.

## 🧭 قرار معماري 3: `ITranslationService` — لأ، مش دلوقتي

- **اخترنا:** الـ controller بياخد الـ **class نفسه** في الـ constructor، من غير interface
- **البدائل:** `ITranslationService` + implementation واحدة
- **ليه:** الـ interface بتكسب حاجة واحدة — seam تستبدل منه. عندك دلوقتي implementation واحدة، ومفيش tests، ومفيش مزوّد تاني. يعني بتدفع ملف زيادة + قفزة في الـ navigation مقابل صفر.
- **إمتى هتضيفها فعلاً:** أول ما تحصل واحدة من التلاتة دول — (١) تكتب test للـ controller من غير ما تنادي Gemini بفلوس، (٢) يبقى فيه مزوّد تاني تختار بينه وبين Gemini في الـ runtime، (٣) تعمل decorator (caching أو logging) حواليه. ساعتها الـ extract interface refactor في الـ IDE بياخد ثانيتين. **الفرق إنك ساعتها هتعرف الـ interface تبقى شكلها ايه، دلوقتي هتخمّن.**
- **الغلطة الشائعة عندك تحديداً:** انت جاي من Clean Architecture حيث كل datasource ليها abstract class. في Flutter ده منطقي لأن الـ `TranslationRepositoryImpl` بيختار بين ML Kit و AI في الـ runtime — **seam حقيقي**. هنا مفيش اختيار. متنقلش الشكل من غير السبب.

---

> 🔒 **Definition of Submitted** — قبل ما تقول "خلصت": (١) افتح آخر review واعدّ الملاحظات، (٢) افتح الملف ده واعدّ الـ acceptance criteria، (٣) `dotnet format` + `git status`.


---

## 📜 السجل (اتنقل من progress.md في 2026-09-23 — النص زي ما هو)

| TASK-003 | الـ AI proxy الحقيقي — Gemini + typed HttpClient + user secrets | AI proxy | ✅ Done | Approved في r4. الـ boundary اتقفل صح في الآخر، والـ error handling اتاختبر على الحقيقي (503 + 429 من Gemini) وعدّى. الـ rounds التلاتة الأولى كلها كانت **نفس الدرس** بتلات أشكال — مين المسؤول عن معرفة ايه |

## ملاحظات من TASK-003 تتبني عليها بعدين

- **⚠️ تصحيح (2026-08-19): الـ free tier بتاع Gemini = 20 request/*اليوم*، مش الدقيقة.** الـ quota خلصت أثناء review TASK-004 والـ log قال بالنص: `quotaId: GenerateRequestsPerDayPerProjectPerModel-FreeTier` · `quotaValue: 20` · `model: gemini-3.7-flash`. **الأثر:** الـ free tier مش كفاية حتى للـ manual testing اليومي — يعني موضوع الـ billing/paid tier بقى عايق للتطوير نفسه، مش بس feature مؤجلة. يتناقش قبل M3.
- `gemini-flash-latest` alias بيتحل لـ `gemini-3.7-flash` النهاردة. **يتثبت قبل الإنتاج.**
- Gemini بيرجّع `finishReason` (`SAFETY` / `MAX_TOKENS` / `RECITATION`) وإحنا مش عاملينه deserialize — ده اللي هيفسّر الترجمات الفاضية.
- `[ApiController]` + `required` بيدوا `ProblemDetails` كاملة بأسماء الحقول الناقصة مجاناً — اتأكد عملياً. نبني عليها في M4 بدل ما نبدأ من الصفر.

**درس اتزرع ولسه هيتحصد:** `char` في C# = UTF-16 code unit مش حرف. اتبيّن عملياً في stub الترجمة (الإيموجي بقى `��` بعد `Array.Reverse`). نرجعله لما نحسب استهلاك الحروف للـ quota — `StringInfo` / `Rune`.

- 🔴 **بيبعت الشغل للمراجعة من غير ما يطبّق ملاحظات الـ review السابقة — 3 مرات، اتفعّلت.**
  - TASK-001 r1 → `status` ناقص
  - TASK-001 r2 → نفس `status` لسه ناقص بعد ما اتقال صريح
  - TASK-002 r1 → تلات ملاحظات في رسالة واحدة (`DateTimeOffset` / namespace / `record` mutable)، طبّق واحدة وبعت "خلصت"
  - **التدخّل:** قاعدة "Definition of Submitted" فوق بدل تاسك مخصصة — المشكلة process مش معرفة. هو عارف `DateTimeOffset`، بس مش بيرجع للرسالة.
  - **✅ تحسّن في TASK-003:** طبّق الملاحظات كلها round بعد round، ولما ساب واحدة **قال ليه** (الـ `ApiKey` في `appsettings.json`) — وده بالظبط اللي القاعدة طالباه. القاعدة شغالة، تفضل.

- **بيعترض بحجة لما يكون معاه حق (TASK-003).** رفض يشيل `ApiKey` من `appsettings.json` وقال السبب: توثيق شكل الـ configuration + مفيش حد تاني على المشروع. حجة سليمة واتقبلت. **درس للـ mentor:** متعلّقش ملاحظة process كبيرة على أضعف نقطة في الليستة — ده بيحوّل النقاش عن الموضوع.
- **بيوصل للحل الصح لو الشرح فيه "ليه" مش "ايه".** في TASK-003 محتاج 4 rounds، بس كل round كان بيتحرك خطوة حقيقية لما السبب اتشرح بمثال عملي (سيناريو 429) بدل قاعدة مجردة.

