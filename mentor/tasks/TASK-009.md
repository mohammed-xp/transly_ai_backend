# 🎫 TASK-009 — الحدود بتاعة `TranslateAsync` بتكذب: سدّها، وخلّي الـ stub يقرا الـ request

**Milestone:** M7 — Testing (آخر تاسك فيها قبل ما نرجع لـ M3) · **الوقت المتوقع:** ~90 دقيقة · **الصعوبة:** ▓▓▓▓░

---

## الهدف

حاجتين مربوطين ببعض:

1. **`TranslateAsync` مبقاش بيرمي خالص.** الـ return type بتاعه `Task<TranslationOutcome>` بيدّعي إنه بيعدّد كل حالات الفشل — دلوقتي فيه على الأقل حالة واحدة بتعدّي من تحته. الادعاء يبقى صحيح.
2. **الـ stub يقرا الـ `HttpRequestMessage` مش يتجاهله.** لحد دلوقتي كل التيستات على **اللي راجع**. الجزء اللي فيه logic حقيقي — بناء الـ prompt، الـ tone switch، حقن اللغات، `store: false` — **مالوش ولا تيست واحد**.

---

## ليه دلوقتي

في TASK-008 بنيت آلة بتخليك تشوف أي رد من Gemini من غير ما تلمسه. التاسك دي هي **أول استخدام حقيقي للآلة**: تصلّح بيها bug اتكشف بسببها، وتغطّي بيها الاتجاه التاني من الـ contract.

**الـ bug اللي اتكشف:** رد بـ `Content-Type: application/json; charset=<حاجة غير معروفة>` بيخلّي `ReadFromJsonAsync` يرمي `InvalidOperationException` وهو بيحوّل الـ charset لـ `Encoding` — يعني **قبل** ما يوصل للـ JSON parser أصلاً. الـ `catch (JsonException)` في `GeminiApiService.cs:101` مش شايفها.

الأثر عند العميل: **500 بدل 502**. و ده يقفل تصنيف الفشل اللي اتبنى في TASK-005 على ثغرة.

**والجزء التاني ليه مهم:** الـ `toneInstruction` switch والـ prompt template في `GeminiApiService.cs:29-47` هو **الوحيد** في الـ service اللي فيه قرار حقيقي، وهو **الوحيد** اللي مفيش حاجة بتحرسه. أي حد يعدّل الـ prompt بعد شهرين مش هيعرف إنه كسر حاجة.

> ⚠️ **عكس TASK-008 تماماً:** هنا **مطلوب** تعدّل `TranslyAI.Api/`. الفرق إن التعديل بيصلّح سلوك، مش بيفتح الكود عشان يتختبر.

---

## المطلوب

### أ. الـ stub يتطوّر

- [ ] `StubHttpMessageHandler` ياخد **`Func<HttpRequestMessage, HttpResponseMessage>`** بدل `HttpResponseMessage` جاهز
- [ ] بيحتفظ بالـ request اللي وصله عشان التيست يـ assert عليه بعد الـ call
- [ ] كل التيستات الستة القديمة لسه خضرا بعد التغيير

> 🔎 ليه: الـ instance الواحد اللي بيترجّع في كل `SendAsync` بيتعمله `Dispose` جوّه `using (httpResponse)` في `GeminiApiService.cs:76`. شوف "السؤال بالتجربة" تحت — **اعمل ده الأول قبل ما تغيّر حاجة**.

### ب. تيستات على الـ request

- [ ] **`[Theory]` + `[InlineData]`** على النبرات التلاتة: كل `TranslationTone` بيوصّل الـ instruction الصح جوّه الـ body
  - أول parameterized test ليك — دي `[Theory]` مش تلات `[Fact]`
- [ ] الـ request بيروح على `v1beta/interactions` بـ POST، والـ absolute URI مبني على الـ `BaseAddress`
- [ ] header `x-goog-api-key` فيه الـ key اللي من `GeminiOptions`
- [ ] الـ body فيه `model` الجاي من الـ options، و **`store: false`**
  - `store: false` كان **قرار واعي** منك في TASK-007 (ماتخزّنش نص المستخدم عند Google). دلوقتي مفيش حاجة بتمنع حد يشيله. ده بالظبط نوع القرار اللي التيست بيتكتب عشانه.
- [ ] الـ source والـ target language بيوصلوا للـ prompt

### ج. سدّ الحدود

> ✏️ **اتصحح 2026-08-27** بعد ما البديل الصح اتحدد (شوف الـ decision block). **الـ assert المطلوب اتغيّر** — اقرا البند كويس.

- [ ] تيست بيدّي رد `200` + `Content-Type: application/json; charset=<قيمة مش موجودة>` + **body سليم تماماً** → الناتج **`Success`**
  - **مش `UpstreamError`.** البايتات JSON صح؛ اللي بايظ هو الـ **label** الجاي من proxy مالوش علاقة بينا. رد كويس مايتحرقش عشان header غلط.
  - **اكتبه الأول وشغّله.** هيبقى **أحمر** — بس مش بـ assertion failure، بـ **exception طالعة من الميثود**. ده الأحمر اللي بيحصل من نفسه من غير ما حد يكسر سطر.
- [ ] تيست تاني: رد **فشل** (`500` مثلاً) بنفس الـ charset البايظ → `UpstreamError` من غير ما ترمي
  - ده **التسريب التاني**، في `GeminiApiService.cs:81`
- [ ] صلّح `GeminiApiService` — **التسريبين** — والتيستين يبقوا أخضر
- [ ] **امسك أي تغيير في اللوج بتيست.** body فاضي حالياً بيرجّع `null` وبيتلوج "empty body"؛ لو الإصلاح حوّله لـ `JsonException` يبقى اللوج اتغيّر. أي تغيير في اللوج لازم يبقى وراه تيست — نفس درس تيست #5 في TASK-008

### د. حجّة الـ `catch`

- [ ] جنب الـ `catch` (أو الـ catches) اكتب **سطر واحد** بيقول: إزاي عرفت إن دي كل الأنواع اللي ممكن تطلع من السطر ده؟
  - لو الإجابة "افترضت" — يبقى الحل غلط والـ decision block تحت فيه بديل

---

## خارج الـ scope

- ❌ **`WebApplicationFactory`** — لسه مؤجلة. الـ layer بتاع outcome → status code تاسك لوحدها في M4.
- ❌ **retries / Polly / `AddStandardResilienceHandler`** — حاجة حقيقية ومطلوبة، بس مش دلوقتي.
- ❌ **تيستات للـ controller أو `LanguageCatalog` أو الـ validation.**
- ❌ **إعادة كتابة الـ prompt نفسه.** انت بتحرسه، مش بتحسّنه.
- ❌ **الديون الخمسة المؤجلة** (`TargetLanguage` PascalCase، `differnt`، إلخ) — سايبينها لـ M4 زي ما اتفقنا.

---

## مفاتيح تدور بيها

**الـ stub بالـ Func**
- الـ lambda بتاخد `HttpRequestMessage` وترجّع `HttpResponseMessage` **جديد في كل نداء**
- قراية الـ body جوّه `SendAsync`: `request.Content` بيبقى `JsonContent` — و `ReadAsStringAsync()` عليه ينفع أكتر من مرة هنا لأنه مش stream جاي من الشبكة

**assert على JSON**
- `JsonDocument.Parse` + `RootElement.GetProperty(...)` — أنضف من مقارنة strings
- أو `JsonSerializer.Deserialize<JsonElement>`

**`[Theory]`**
```csharp
[Theory]
[InlineData(TranslationTone.Formal, "formal")]
public async Task ...(TranslationTone tone, string expectedFragment)
```

**الحدود**
- 📎 [HttpContent.ReadFromJsonAsync — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.json.httpcontentjsonextensions.readfromjsonasync)
- `HttpContent.ReadAsStringAsync` · `JsonSerializer.Deserialize<T>(string, JsonSerializerOptions)`
- `JsonSerializerDefaults.Web` — لو رحت لمسار الـ string، خلي بالك إن `ReadFromJsonAsync` بيستخدم options مش زي الـ default

---

## Definition of Done

```
dotnet test
```

كل التيستات خضرا، **0 warnings**، `dotnet format --verify-no-changes` نضيف.

وجوّه التسليم: **الـ output الأحمر للتيست بتاع الـ charset قبل ما تصلّح الـ service.** ده مش بند إضافي — ده بيحصل من نفسه في نص الشغل، الصقه وانت مروّح.

---

## 🧭 قرار معماري: إزاي تسد حدود بتعتمد على معرفة كل الـ exceptions؟

> ✏️ **اتصحح 2026-08-27.** النسخة الأولى من القسم ده كانت بتقول إن البديل (ج) — "اقرا الـ body كـ `string` الأول" — بيحل المشكلة. **غلط، وأنا اللي غلطت.** `ReadAsStringAsync` بيحوّل الـ charset لـ `Encoding` هو كمان وبيرمي **نفس** الـ `InvalidOperationException`. اتحقق عملياً. البديل الصح اتضاف كـ (د).

- **البدائل:**
  - **أ.** تزوّد `catch (InvalidOperationException)` جنب الـ `JsonException`
  - **ب.** `catch (Exception)` وخلاص
  - **ج.** ~~تقرا الـ body كـ `string` الأول (`ReadAsStringAsync`)~~ — **مبيشتغلش**، شوف فوق
  - **د.** تقرا الـ body كـ **stream** أو **bytes** (`ReadAsStreamAsync` / `ReadAsByteArrayAsync`) وبعدين `JsonSerializer.DeserializeAsync` عليه
- **الاختيار سايبهالك** — بس الإجابة لازم تعدّي من الاختبار ده: **إيه اللي بيضمن إن السطر ده مش هيرمي حاجة رابعة السنة الجاية؟**
- **القاعدة اللي (ج) كشفتها وهي بتفشل:** كل الـ APIs **النصية** على `HttpContent` بتقرا الـ `charset` من الـ header وبتحوّله لـ `Encoding` قبل أي حاجة. الـ APIs بتاعة **البايتات/الـ stream** مبتبصش على الـ header ده خالص. الفرق ده مش مكتوب في اسم الميثود ولا في الـ signature — بيتعرف بالتجربة أو بقراية الـ source.
- **⚠️ فيه تسريبين مش واحد:** `GeminiApiService.cs:81` بيعمل `ReadAsStringAsync` على الـ error body عشان يلوجّه — يعني **مسار الفشل نفسه بيسرّب**. أي حل لازم يغطي الاتنين.
- **(أ) بتصلّح الحالة، مش النوع.** انت دلوقتي عارف اتنين لأنك جرّبت. `ReadFromJsonAsync` بيلف: قراية الـ headers، تحويل الـ charset، قراية الـ stream، والـ deserialization — أربع مصادر مختلفة تحت غطا واحد. لسه بتفترض، بس بافتراض أوسع شوية.
- **(ب) هي نفس ملاحظة TASK-005 r1** — الـ `catch (Exception)` اللي كان بيرجّع `ex.Message` للعميل. الفرق إن ساعتها كان بيسرّب داخليات؛ هنا هيبلع bugs حقيقية في كودك انت (`NullReferenceException` مثلاً هترجع `UpstreamError` وكأن Gemini هو اللي غلطان). **الـ catch الواسع مش بيسد حدود، هو بيمسح الفرق بين "هُم غلطانين" و"إحنا غلطانين".**
- **(ج) بتغيّر السؤال.** بدل "إيه اللي ممكن يرميه الـ helper؟" بتبقى "إيه اللي ممكن يرميه `Deserialize` على string أنا ماسكه؟" — والإجابة دي **موثّقة ومحدودة**. التكلفة: بتحمّل الـ body كله في الذاكرة، وبتفقد الـ streaming. على ردود ترجمة (كيلوبايتات) دي مش تكلفة؛ على رد 50MB هتبقى.
- **الغلطة الشائعة:** الاعتقاد إن الـ `catch` الضيّق دايماً أأمن من الواسع. الضيّق **بيوقع صح** (الـ exception بتوصل لحد ما حد يشوفها)؛ الواسع **بيوقع صامت**. بس الاتنين بيفشلوا لو الميثود بتوعد بحاجة (`TranslationOutcome`) مش قادرة توفيها. **الحل الحقيقي بيقلّل عدد الحاجات اللي ممكن ترمي، مش بيزوّد عدد الـ catches.**

---

## 🪤 اللي بتراجع عليه في الـ review

1. **`catch (Exception)`** — شوف فوق. لو اخترتها لازم تكتب ليه، والسبب لازم يرد على الفقرة دي بالذات.
2. **assert على الـ prompt بـ `Assert.Contains("formal")`** والـ instruction الحقيقي فيه كلمة `formal`? راجع `GeminiApiService.cs:31` — الـ fragment اللي بتدوّر عليه لازم يفرّق النبرات عن بعض فعلاً، مش يبقى موجود في تلاتتهم.
3. **التيستات القديمة اتكسرت** وانت بتغيّر الـ stub، فصلّحت الـ stub لحد ما تعدّي بدل ما تفهم ليه وقعت.
4. **الـ `[Theory]` اتكتبت تلات `[Fact]`** — شغّالة، بس فاتتك النقطة.

---

## ❓ سؤال تجاوب عليه بالتجربة (مش بافتراض)

**اعمل ده قبل ما تلمس الـ stub:**

اكتب تيست بينادي `TranslateAsync` **مرتين** على نفس الـ `GeminiApiService` (نفس الـ stub القديم اللي ماسك `HttpResponseMessage` واحد).

- النداء الأول بيرجّع إيه؟ والتاني؟
- الـ exception (لو طلعت) جاية من **كودك** ولا من **الـ stub**؟ وليه دي أسوأ حاجة ممكن تحصل في test suite؟
- بعد ما تغيّر الـ stub لـ `Func<>`، شغّل نفس التيست تاني. اتغيّر إيه، وليه؟

الدرس اللي بيطلع من ده اسمه **ملكية الـ `IDisposable`** — نفس درس TASK-006 بس من الناحية التانية: هناك عملت dispose لحاجة مش بتاعتك؛ هنا الـ stub بيوزّع object واحد على كل الـ callers وكل واحد فيهم فاكر إنه بيملكه.

---

## 📋 شكل التسليم

```
### أ. الـ stub
- [ ] Func<HttpRequestMessage, HttpResponseMessage>        →
- [ ] بيحتفظ بالـ request                                  →
- [ ] الستة القدام لسه خضرا                                →

### ب. الـ request
- [ ] [Theory] على النبرات التلاتة                         →
- [ ] URL + method                                         →
- [ ] x-goog-api-key                                       →
- [ ] model + store:false في الـ body                      →
- [ ] اللغات في الـ prompt                                 →

### ج. الحدود
- [ ] تيست الـ charset (الصق الأحمر قبل الإصلاح)           →
- [ ] الإصلاح → أخضر                                       →
- [ ] اللوج بيفرّق بين الحالتين                            →

### د. الحجّة
- [ ] سطر جنب الـ catch: إزاي عرفت دول كل الأنواع؟         →

### السؤال
- [ ] النداء مرتين — قبل وبعد                              →

### Definition of Submitted
- [ ] فتحت آخر رسالة review وعدّيت الملاحظات
- [ ] فتحت ملف التاسك ده وعدّيت الـ acceptance criteria
- [ ] dotnet format + git status
```


---

## 📜 السجل (اتنقل من progress.md في 2026-09-23 — النص زي ما هو)

| TASK-009 | سدّ حدود `TranslateAsync` + الـ stub يقرا الـ request + أول `[Theory]` | M7 | ❌ **اتلغت بطلبه** (2026-08-27) | "مش مقتنع بالتاسك دي · قم بتبديلها". اتبدّلت من غير تفاوض. **الخطأ كان مني**: M7 اتقدّمت لسبب محدد، اتحقق في TASK-008، وكان لازم أرجع للـ curriculum فوراً بدل ما أكمّل بالعطالة. الشغل الفني اللي كان فيها اتسجّل كدين (تسريب الـ charset) وموعده M4 |

- **❌ TASK-009 اتلغت بطلبه** (2026-08-27): **"مش عايز اعمل التعديلات دي · مش مقتنع بالتاسك دي · قم بتبديلها"**. اتبدّلت من غير تفاوض. الملف `mentor/tasks/TASK-009.md` متساب للتاريخ.
  - **الدرس للـ mentor:** M7 اتقدّمت بقرار مني عشان نمط متكرر، والغرض منها اتحقق في TASK-008. **تاسك تانية ورا بعض في نفس الموضوع كانت زيادة** — التاسك التانية بقت "خلّص اللي فات" مش "شغل جديد"، وهو حسّها كده وهو محق. الإشارة كانت موجودة من رسالته الأولى في الجلسة ("خلصت تاسك 8 وعايز تاسك جديدة") ومقريتهاش.
  - **قاعدة تتضاف:** بعد ما milestone تتقدّم عشان سبب محدد وتحقق سببها → **ارجع لمسار الـ curriculum فوراً**، متكمّلش فيها بالعطالة.

### 💳 دين مفتوح — تسريب exception من `GeminiApiService` (اتأجل 2026-08-27)

> اتحوّل لـ TASK-009، وTASK-009 اتلغت بطلبه. **الدين قايم ومسجّل بتكلفته**، وميترجعلوش كتاسك مستقلة.

| البند | المكان | التكلفة |
|---|---|---|
| `ReadFromJsonAsync` بيرمي `InvalidOperationException` على charset مش معروف — `catch (JsonException)` مش شايفها | `GeminiApiService.cs:99` | **500 بدل 502** عند العميل. بيثقب تصنيف الفشل بتاع TASK-005 |
| `ReadAsStringAsync` على الـ error body بيرمي نفس الحاجة | `GeminiApiService.cs:81` | نفس الأثر، على مسار الفشل |
| الـ prompt building (tone switch + حقن اللغات) + `store: false` مالهمش أي حارس | `GeminiApiService.cs:29-54` | `store: false` قرار خصوصية اتاخد في TASK-007 ومحدش هيلاحظ لو اتشال |

**الحل المتحقق منه عملياً (لو رجعنا له):** `ReadAsStreamAsync` + `JsonSerializer.DeserializeAsync` بدل `ReadFromJsonAsync` (الـ stream مبيبصش على الـ charset)، و`ReadAsByteArrayAsync` + `Encoding.UTF8.GetString` بدل `ReadAsStringAsync`.
**📍 الموعد:** M4 مع الـ `IExceptionHandler` المركزي — هناك بيتحل لكل الـ endpoints مرة واحدة.

**🔎 قاعدة اتعلمناها وتتسجّل:** كل الـ APIs **النصية** على `HttpContent` (`ReadAsStringAsync` / `ReadFromJsonAsync`) بتحوّل الـ `charset` الجاي في الـ header لـ `Encoding` قبل أي حاجة، وبترمي `InvalidOperationException` لو مش معروف. الـ APIs بتاعة **البايتات/الـ stream** مبتبصش على الـ header ده خالص. الفرق مش ظاهر في الـ signature.

**🔴 اللي طلع من الـ review:** `ReadFromJsonAsync` بيرمي **`InvalidOperationException("The character set provided in ContentType is invalid.")`** لما الـ `charset` في الـ `Content-Type` مش معروف — بتترمي وهو بيحوّل الـ charset لـ `Encoding`، يعني **قبل** الـ serializer (اتأكدت عملياً: الحالات التانية وصلت للـ serializer، دي لأ). الـ `catch (JsonException)` في `GeminiApiService.cs:101` مش شايفها → **بتطلع برّه `TranslateAsync`** → **500 عند العميل بدل 502**. ده بيثقب تصنيف الفشل اللي اتبنى في TASK-005.

> **⚠️ درس الجلسة دي — يتقرا قبل كتابة أي ticket جاي:** M7 اتقدّمت بقرار لسبب محدد (نمط متكرر 3 مرات)، السبب اتحقق في TASK-008، وأنا كتبت TASK-009 في نفس الموضوع بالعطالة. **رفضها، وكان محقاً.** الإشارة كانت في رسالته الأولى ("خلصت تاسك 8 و**عايز تاسك جديدة**"). القاعدة: milestone بتتقدّم لسبب → أول ما السبب يتحقق، ارجع لمسار الـ curriculum.

