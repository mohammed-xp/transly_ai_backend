# 🎫 TASK-008 — أول tests: تخلي مسارات الفشل في `GeminiApiService` تتنفّذ فعلاً

**Milestone:** M7 — Testing (اتقدّمت بقرار — السبب تحت) · **الوقت المتوقع:** ~120 دقيقة · **الصعوبة:** ▓▓▓░░

---

## الهدف

مشروع tests شغال جنب الـ API، بيقدر يخلّي `GeminiApiService` يشوف **أي** رد من Gemini — رد ناجح، 429، JSON بايظ، status مش معروف — **من غير ما يلمس Gemini ولا يخصم request واحد من الـ quota**، وبيثبت إن كل مسار من دول بيتنفّذ فعلاً وبيرجّع الـ `TranslationOutcome` الصح.

---

## ليه دلوقتي

**سببين، والاتنين مش نظريين.**

### 1. تلات مرات كتبت دفاع مش قادر يشتغل — والتلاتة عدّوا بصفر warnings

| # | التاسك | اللي حصل |
|---|---|---|
| 1 | TASK-005 | guard على `candidates` والمسار مبيعدّيش عليه أصلاً — `JsonException` بترمي قبله |
| 2 | TASK-006 | ميثود اسمها `Validation` من غير `: IValidatableObject` → الـ framework عمره ما نداها |
| 3 | TASK-007 | `Unkown` و `Unknown` في نفس الـ enum → التحذير عمره ما هيطلع |

القاسم المشترك: **الكود بيـ compile صح في التلاتة**. الـ compiler بيسأل "هل ده مكتوب صح؟" ومبيسألش "هل ده هيتنفّذ؟".

الحاجة الوحيدة اللي بتسأل السؤال التاني هي **إنك تخلي الحالة تحصل**. لحد دلوقتي ده كان بيتعمل يدوي — تشغّل، تبعت request، تبص على اللوج. وده اشتغل في الحالات اللي فكّرت فيها، وفشل في التلات حالات اللي فوق **لأنك مقدرتش أصلاً تخلّي الحالة تحصل**: مش هتقنع Gemini يرجّعلك `"status": "bogus"` ولا JSON بايظ ولا 429 وقت ما انت عايز.

الـ test هنا مش للـ coverage. الـ test هو **الآلة اللي بتخليك تشغّل المسار**. اختبار واحد كان هيمسك كل واحدة من التلاتة.

### 2. الـ quota بقت عايق فعلي

الـ free tier = **20 request/يوم**. أثناء review TASK-007 لوحده خلصت **مرتين**. أي تجربة يدوية لمسارات الفشل بتاكل من نفس الـ 20.

التاسك دي **بتاكل صفر requests**. ده مش مكسب جانبي — ده السبب اللي بيخلي التاسك دي تسبق أي شغل جديد على الـ AI.

**ليه اتقدّمت من M7 لهنا؟** الـ curriculum بيحط Testing في M7، والترتيب ده منطقي لما التيستات بتكون "تأمين على refactor". هنا الوضع مختلف: التيستات بقت **أداة تشخيص مطلوبة دلوقتي** لنمط متكرر تلات مرات + عايق quota حقيقي. الخريطة مش عقد.

---

## المطلوب

### أ. المشروع

- [ ] مشروع tests جديد بـ xUnit، مضاف للـ solution (`TranslyAI.slnx`)، وبيـ reference مشروع الـ API
- [ ] `dotnet test` من جذر الـ solution بيشتغل وبيعدّي

### ب. الـ seam

- [ ] **stub لـ `HttpMessageHandler`** بيخليك تحدد الرد اللي `GeminiApiService` هيشوفه: الـ status code، والـ body، والـ headers
- [ ] الـ stub ده هو الحاجة الوحيدة المزيّفة في التيستات. `GeminiApiService` نفسه بيتبني حقيقي بالـ `HttpClient` الحقيقي — انت بس بتتحكم في اللي راجع من الشبكة

> ⚠️ **مفيش سطر واحد يتغيّر في `TranslyAI.Api/` عشان التيستات تعدّي.** لا `internal` بقى `public`، لا method خاصة اتفتحت، لا interface اتستخرجت. لو لقيت نفسك محتاج تعدّل الـ production code عشان تختبره — **قف واسألني**، دي إشارة تصميم مش عقبة ميكانيكية. (في التاسك دي مش هتحتاج — اتأكدت من ده.)

### ج. التيستات (خمسة)

كل واحد بينادي `TranslateAsync` ويـ assert على الـ `TranslationOutcome` الراجع.

- [ ] **1. الطريق الناجح** — رد 200 بـ `status: "completed"` وفيه step نوعه `model_output` → `Success` بالنص الصح واسم الموديل الصح
- [ ] **2. الترتيب مش مهم** — نفس الرد بس الـ `model_output` **مش أول step** (حط قبله step من نوع تاني) → لسه `Success` بنفس النص
  - ده بيقفل الاختيار الصح اللي عملته في TASK-007 (الاستخراج بالنوع مش بالموضع) عشان محدش يكسره بعد سنة
- [ ] **3. 429** → `RateLimited`، و `RetryAfter` **متقري من الـ header** مش `null`
- [ ] **4. 200 بـ body مش JSON صالح** → `UpstreamError`، ومفيش exception طالعة برّه الـ method
  - دي بالظبط النقطة اللي الـ guard في TASK-005 كان في المكان الغلط بسببها
- [ ] **5. `status` مش معروف** (مثلاً `"bogus"`) → التحذير **بيتسجّل فعلاً**
  - ⚠️ ده التيست الوحيد اللي الـ assert بتاعه على **اللوج** مش على القيمة الراجعة. **بقصد** — البج بتاع TASK-007 كان `NotCompleted(Unknown)` راجع صح تماماً، واللي كان بايظ هو التحذير بس. تيست بيـ assert على القيمة الراجعة كان هيعدّي وهو أخضر.
  - يعني: **لما الأثر الوحيد لكودك يكون سطر لوج، اللوج هو السلوك، ولازم يتـ assert عليه.**

### د. الإثبات إن التيستات ممكن تفشل

- [ ] اكسر **سطر واحد** في `GeminiApiService` عمداً (مثال: خلي `"model_output"` تبقى `"model_outputs"`)، شغّل `dotnet test`، **والصق الـ output الأحمر في التسليم**، وبعدين ارجّع السطر
- [ ] كرّرها على تيست تاني من نوع مختلف (واحد من 3/4/5)

> تيست عمره ما اتشاف أحمر = **بالظبط** نفس الـ bug بتاع الـ guard اللي المسار مبيعدّيش عليه. الاتنين كود بيبص عليك وبيقولك "أنا موجود" من غير ما حد يثبت إنه شغال. البند ده هو **قلب التاسك** — لو اتخطى، الـ review بيرجع من غير ما أقرا الباقي.

---

## خارج الـ scope

- ❌ **`WebApplicationFactory` / integration tests** — الـ layer اللي فوق (outcome → status code) تاسك لوحدها. دلوقتي إحنا جوّه الـ service.
- ❌ **تيستات للـ controller أو للـ validation أو للـ `LanguageCatalog`** — مهمين، مش دلوقتي.
- ❌ **NSubstitute** — موجودة في الـ stack المتفق عليه ومش هتحتاجها هنا ولا سطر. لما تحتاجها هقولك السبب.
- ❌ **code coverage** — أي رقم coverage في التاسك دي بيقيس حاجة مش هي المطلوبة.
- ❌ **إعادة هيكلة `GeminiApiService`** عشان "يبقى أسهل في الاختبار". هو أصلاً قابل للاختبار.

---

## مفاتيح تدور بيها

**إنشاء المشروع**
- `dotnet new xunit`
- `dotnet sln add` — لاحظ إن الـ solution هنا `.slnx` مش `.sln` (الفورمات الجديد)
- `dotnet add reference`

**الـ seam**
- `HttpMessageHandler` — الـ class المجرّدة اللي `HttpClient` بيقف عليها. الميثود اللي هتـ override:
  ```csharp
  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  ```
- `new HttpClient(handler)` — الـ constructor اللي بياخد handler
- 📎 [Unit testing HttpClient — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

**بناء الـ service في التيست**
- `NullLogger<T>.Instance` — من `Microsoft.Extensions.Logging.Abstractions`
- `Options.Create(...)` — بيلف object في `IOptions<T>` من غير DI container

**assert على اللوج**
- package: `Microsoft.Extensions.Diagnostics.Testing` → `FakeLogger<T>` و `Collector.GetSnapshot()`
- 📎 [FakeLogger — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator)
- لو مش عاجبك: تقدر تكتب `ILogger<T>` بسيط بإيدك بيجمّع اللي اتسجّل. الاتنين مقبولين — بس **قول ليه اخترت اللي اخترته**

**xUnit vs Dart's `test`**
- `[Fact]` = `test('...')` · `[Theory]` + `[InlineData]` = parameterized test
- مفيش `setUp` — الـ **constructor** بتاع الـ class هو الـ setUp، والـ `IDisposable.Dispose` هو الـ tearDown. instance جديدة لكل تيست.
- الأسماء: `MethodName_Scenario_ExpectedResult` هو الـ convention الشائع في .NET

---

## Definition of Done

```
dotnet test
```

من جذر الـ solution → **5 تيستات، كلهم أخضر، صفر warnings**.

وجنبها في التسليم: **الـ output الأحمر** من البند (د) — مرتين، لتيستين مختلفين.

---

## 🧭 قرار معماري: فين الـ seam بتاع service بيكلّم HTTP؟

- **اخترنا:** stub على `HttpMessageHandler`
- **البدائل:**
  - **أ.** استخراج `IGeminiApiService` وعمل mock ليها
  - **ب.** ضرب Gemini الحقيقي في التيستات
  - **ج.** `WebApplicationFactory` مع استبدال الـ handler في الـ DI
- **ليه الـ handler هنا:** `HttpClient` هو **الحدود** بتاعة الـ service. كل حاجة عايز أختبرها — الـ status code، شكل الـ JSON، الـ headers، الـ timeout — بتدخل من هناك. الـ `HttpMessageHandler` هو أضيق نقطة تقدر تتحكم فيها من غير ما تزيّف أي حاجة من كودك انت. الـ service بيتنفّذ **بالكامل، حقيقي**، وانت بس بتكتب اللي الشبكة بترد بيه.
- **ليه مش (أ):** الـ interface + mock بتختبر **إن الـ mock بيرجّع اللي انت قلتله يرجّعه**. صفر معلومة عن `MapStatus` أو `ExtractText` أو الـ `catch` blocks — يعني التلات bugs اللي التاسك دي اتعملت عشانهم **كلهم كانوا هيعدّوا**. ودي كمان مناقضة لـ **قرار #15**: هناك قلنا الـ interface تتضاف أول ما يبقى فيه test seam حقيقي — والـ seam هنا طلع `HttpMessageHandler` مش الـ interface. القرار #15 **يفضل قايم زي ما هو**.
- **ليه مش (ب):** غير حتمي، بياكل من الـ 20/يوم، **ومستحيل تجبره يرجّعلك JSON بايظ أو `"status": "bogus"`**. يعني بالظبط الحالات اللي محتاج تختبرها هي اللي مش هتقدر تعملها.
- **إمتى (ج) بتبقى الصح:** لما السؤال يبقى "الـ outcome ده بيطلع 502 ولا 504؟" — ده سلوك الـ controller والـ pipeline، مش سلوك الـ service. تاسك جاية.
- **الغلطة الشائعة:** محاولة عمل mock لـ `HttpClient` نفسه. هو `class` والميثودز بتاعته مش `virtual` — مكتبات الـ mocking مش هتعرف. الناس بتقضي ساعة تحارب في ده قبل ما تكتشف إن الـ handler هو المدخل.

---

## 🪤 اللي بتراجع عليه في الـ review

1. **تعديل production code عشان التيست يعدّي** — أي `internal`/`public` جديدة، أو interface مستخرجة. البند (ب) بيقول قف واسأل، والإجابة معروفة: مش محتاج.
2. **تيست #5 بيـ assert على القيمة الراجعة** بدل اللوج → التيست هيعدّي وهو أخضر مع البج نفسه اللي اتعمل عشانه. لو حصل ده، التاسك كلها اتحوّلت لطقوس.
3. **البند (د) اتخطى** أو اتعمل مرة واحدة بدل مرتين → الـ review بيقف.
4. **assert ضعيف** — `Assert.NotNull(outcome)` مش تيست. الـ assert لازم يفرّق بين الحالة دي وأي حالة تانية.

---

## ❓ سؤال تجاوب عليه بالتجربة (مش بافتراض)

> ✏️ **اتصحح 2026-08-26.** النسخة الأولى من السؤال ده كان فيها تلميح غلط مني (قلت "بصّ على الـ `Content-Type`" وأنا مفترض إن الـ media type بيتفحص). اتحققت منه عملياً وطلع **مش صحيح** — `HttpContent.ReadFromJsonAsync` **مبيفحصش الـ media type خالص**. السؤال اتعاد صياغته على أساس النتيجة الحقيقية.

`catch (JsonException)` في `GeminiApiService.cs:101` بيغطي إيه بالظبط؟

جرّب التلاتة في الـ stub وقول اللي حصل:

| # | الرد | التوقع بتاعك قبل ما تجرّب |
|---|---|---|
| 1 | `Content-Type: application/json` + body `{ "status": "completed",` (مقصوص) | ؟ |
| 2 | `Content-Type: text/html` + body `<html>502</html>` | ؟ |
| 3 | `Content-Type: application/json; charset=klingon-9` + body `{}` سليم | ؟ |

- في أنهي حالة الـ `catch` بيشتغل؟
- في أنهي حالة exception بتطلع **برّه** الميثود خالص — يعني `TranslateAsync` بتكذب على الـ return type بتاعها؟
- الـ pattern اللي يطلع من ده: **`catch` على نوع واحد = افتراض إن ده النوع الوحيد اللي ممكن يطلع من السطر ده.** إزاي تتأكد من الافتراض ده بدل ما تفترضه؟

---

## 📋 شكل التسليم

انسخ ده واملاه — بند بند، وجنب كل واحد رقم السطر أو الـ output:

```
### أ. المشروع
- [ ] مشروع tests + مضاف للـ .slnx + reference للـ API   →
- [ ] dotnet test بيشتغل                                  →

### ب. الـ seam
- [ ] stub لـ HttpMessageHandler                          →
- [ ] صفر تعديلات في TranslyAI.Api/  (git diff --stat)    →

### ج. التيستات
- [ ] 1. الطريق الناجح                                    →
- [ ] 2. model_output مش أول step                         →
- [ ] 3. 429 + RetryAfter متقري من الـ header             →
- [ ] 4. JSON بايظ → UpstreamError                        →
- [ ] 5. status مجهول → التحذير اتسجّل (assert على اللوج)  →

### د. الإثبات
- [ ] كسرت سطر → output أحمر (الصقه)                      →
- [ ] كسرت سطر تاني → output أحمر (الصقه)                 →
- [ ] رجّعت الاتنين (git status نضيف)                     →

### السؤال
- [ ] الفرق بين النصين + ليه                              →

### Definition of Submitted
- [ ] فتحت آخر رسالة review وعدّيت الملاحظات
- [ ] فتحت ملف التاسك ده وعدّيت الـ acceptance criteria
- [ ] dotnet format + git status
```
