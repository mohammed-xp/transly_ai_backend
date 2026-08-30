# .NET Mentor — Progress

> ملف الحالة بتاع الـ mentor. بيتقرا في أول كل جلسة وبيتحدث في آخرها.
> مكانه: `mentor/progress.md` في الريبو بتاع الباك اند.

**آخر تحديث:** 2026-08-27

---

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

## الـ Stack المتفق عليه

| العنصر | الاختيار | ليه |
|---|---|---|
| .NET | 10 (net10.0) | LTS، ومتسطب عنده أصلاً (SDK 10.0.302) والمشروع متظبط عليه |
| API style | Controllers (+ minimal endpoints للـ infrastructure زي `/health`) | الـ API رايح على auth + subscriptions + admin، والـ controllers بتستوعب النمو ده |
| Database | ✅ **MySQL** (اتحدد 2026-08-27 باختياره) | مرتاح فيها أصلاً. الحاجة الجديدة في M3 لازم تكون EF Core مش الداتابيز |
| MySQL provider | ✅ **`MySql.EntityFrameworkCore` 10.0.9** (Oracle الرسمي) | ⚠️ **مش Pomelo** — آخر نسخة فيها 9.0.0 ومفيش EF Core 10، والمشروع `net10.0`. اتأكدت من nuget. الفخ إن Pomelo هي اللي في كل الـ tutorials |
| Docker | ⏳ مؤجّل لـ M8 | سأل "ضروري دلوقتي؟" — لأ. محتاج database مش container. قيمة Docker (بيئة بتتمسح وتتبني في ثانية) مش مشكلة قايمة عنده |
| ORM | EF Core | الـ default في الـ ecosystem، وبيدي code-first migrations |
| Auth | ⏳ JWT (التفاصيل في M5) | متوقف على إذا التطبيق فيه Firebase Auth ولا لأ |
| Testing | xUnit + NSubstitute | الـ default في .NET |
| Deployment | ⏳ Docker + GitHub Actions (M8) | يتحدد الـ host وقتها |

## 🔴 قاعدتين جديدتين اتفرضوا منه (2026-08-24) — تسري على كل الجلسات الجاية

1. **الـ API client-agnostic تماماً.** متقراش ولا تتأثر بكود تطبيق الفلاتر ولا أي front end. الشكل اللي الكلاينت عامل بيه الحاجة **مش معيار**. الوحيد اللي يتحسب: إن الـ endpoints مستهدفة موبايل (payload، latency، pagination، error contract، versioning). لو ناقصني requirement — **أسأله بالكلام**، مش أقرا الفلاتر.
   - **الأثر الفوري:** قسم "الـ Contract المستخرج من تطبيق الفلاتر" تحت بقى **أرشيف تاريخي**، مش مرجع. `Language.wellKnown` و `HistoryEntry` وغيرهم مايتنسخوش — أي contract جاي يتصمم من الصفر.
2. **كلمة "ساعدني" = الكود كامل + الشرح في الدردشة.** مش hint ladder، مش أسئلة توجيهية. الـ hint ladder بتاعة الـ skill تفضل شغالة على "متعلق"/"مش فاهم"، بس **"ساعدني" بتقفز على طول للمستوى الكامل** — الكود اللي المفروض يكتبه أو يعدّله + ليه. في الدردشة (code block)، مش تعديل مباشر على ملفاته.
   - **إضافة اتفق عليها:** بعد الكود، أسأله سؤال أو اتنين عليه. وكلمة **"لمحة"** اختيارية لو عايز دفعة بس.

---

## الوضع الحالي

- **Milestone:** **M3 — Data Layer** (رجعنا لمسار الـ curriculum بعد ما سبب تقديم M7 اتحقق).
- **التاسك المفتوحة:** 🛑 **مفيش — TASK-011 اتقفلت.** استنى طلبه الصريح قبل ما تتكتب تاسك جديدة (القاعدة الثابتة تحت).
- **اللي قبلها:** ✅ **TASK-011 اتقفلت (Approved، 2026-08-30)** — الكاش بقى يعرف بأي موديل اتولد كل صف. الكوميت `ab4c389 fix: key the translation cache by model, not just content (TASK-011)`.
- ✅ TASK-010 اتقفلت واتكوميتت — `4e893d7 feat: cache Gemini translations in MySQL to cut API calls (TASK-010)`. الـ working tree نضيف.
- **❌ TASK-009 اتلغت بطلبه** (2026-08-27): **"مش عايز اعمل التعديلات دي · مش مقتنع بالتاسك دي · قم بتبديلها"**. اتبدّلت من غير تفاوض. الملف `mentor/tasks/TASK-009.md` متساب للتاريخ.
  - **الدرس للـ mentor:** M7 اتقدّمت بقرار مني عشان نمط متكرر، والغرض منها اتحقق في TASK-008. **تاسك تانية ورا بعض في نفس الموضوع كانت زيادة** — التاسك التانية بقت "خلّص اللي فات" مش "شغل جديد"، وهو حسّها كده وهو محق. الإشارة كانت موجودة من رسالته الأولى في الجلسة ("خلصت تاسك 8 وعايز تاسك جديدة") ومقريتهاش.
  - **قاعدة تتضاف:** بعد ما milestone تتقدّم عشان سبب محدد وتحقق سببها → **ارجع لمسار الـ curriculum فوراً**، متكمّلش فيها بالعطالة.
- **اللي قبلها:** ✅ **TASK-008 اتقفلت (2026-08-27)** — الفخّين الأساسيين عدّاهم، 6/6 أخضر، صفر تعديل في الـ production code. **بندين اتأجلوا بقراره الصريح** ("انا جربت كل البتقولو والشغل سليم") — التفاصيل تحت. الكوميت `77bba27`.

### 💳 دين TASK-008 — مؤجل بقراره الصريح (2026-08-27)

> قال **"انا جربت كل البتقولو والشغل سليم نبدأ تاسك جديدة"** بعد ما الـ review طرح البندين بتكلفتهم. **دي مش بنود متخطّاة — ده تأجيل واعي**، زي دين TASK-006 بالظبط. ماتتحسبش عليه في مفكرة الأنماط.

| # | البند | الحالة |
|---|---|---|
| 1 | بند (د) — شكل الأحمر (أنهي سطر اتكسر، أنهي تيستات وقعت وأنهي فضلت خضرا) | قال إنه عمله، الـ output مالوش أثر مكتوب. **اتقفل — TASK-009 فيها أحمر بيحصل من نفسه فمش محتاجين تمرين مصطنع.** |
| 2 | الصف التالت من جدول السؤال (`charset` بايظ) | **الإجابة اتقالتله في الـ review** بدل ما يجرّبها، لأن التمرين اتقفل والـ bug حقيقي. **اتحوّل لشغل فعلي في TASK-009 ج.** |
| 3 | 🟡 `StubHttpMessageHandler` بيرجّع نفس الـ instance | **اتحوّل لـ TASK-009 أ** (بند إجباري مش دين) |
| 4 | 🔵 `Assert.DoesNotContain` الزايد · dispose في التيستات · سبب اختيار `FakeLogger` | متسابة. تجميل. |

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

### 🔍 نتيجة review TASK-008 r1 (2026-08-27)

### 🔍 نتيجة review TASK-008 r1 (2026-08-27)

**اتشغّل بنفسي:** `dotnet build --no-incremental` → 0 warnings · `dotnet test` → **6/6 أخضر** (كتب 6 مش 5) · `dotnet format --verify-no-changes` → نضيف · `git diff fa6c556..77bba27 -- TranslyAI.Api/` → **فاضي تماماً**.

**✅ الفخّين الأساسيين عدّاهم:**
- 🪤 **تعديل production code** — صفر. الـ diff على `TranslyAI.Api/` فاضي حرفياً.
- 🪤 **تيست #5 يـ assert على القيمة الراجعة** — عدّاه صح: الـ assert الأول على `_logger.Collector.GetSnapshot()` (`FakeLogger`)، والقيمة الراجعة ثانوية وجنبها تعليق بيقول ليه. ده كان **الفخ الأساسي** في التاسك.
- 🔺 **وكتب تيست سادس مكنش مطلوب** — `When200BodyIsNotJsonAtAll` = الصف التاني من جدول "السؤال بالتجربة" اتحوّل لـ regression guard دايم بدل تجربة بتتنسى. **أحسن حاجة في التسليم.**

**🔴 blockers (r1):**
1. **بند (د) — الإثبات إن التيستات ممكن تفشل: مفيش output أحمر ملصوق، ولا مرة.** ده الغرض من التاسك مش بند فيها. وكمان **checklist "شكل التسليم" فاضية** — ودي كانت التدخّل المتفق عليه بعد 4 مرات تخطّي acceptance criteria (شوف المفكرة، دي الخامسة).
2. **الصف التالت من جدول السؤال (`charset=klingon-9`) متجرّبش والسؤال مجاوبش عليه.** جرّبته بنفسي: **`InvalidOperationException("The character set provided in ContentType is invalid.")`** — بتترمي من `System.Net.Http.Json` **قبل** ما تصل للـ serializer، و `catch (JsonException)` في `GeminiApiService.cs:101` مش بيشوفها → **بتطلع برّه `TranslateAsync` خالص**. يعني الـ return type اللي بيدّعي إنه بيعدّد كل حالات الفشل **بيكذب**.
   - ⚠️ **متقالوش نوع الـ exception في الـ review بقصد** — لازم يشوفها بنفسه.
   - **دي مش نظرية:** أي reverse proxy أو WAF قدام Gemini يقدر يرجّع charset بايظ.

**🟡 Should fix:** `StubHttpMessageHandler` بيرجّع **نفس** الـ `HttpResponseMessage` instance في كل `SendAsync`، والـ service بيعمله `using`. ماشي النهاردة لأن كل تيست بينادي مرة واحدة؛ أول تيست بينادي مرتين هياخد `ObjectDisposedException` **من الـ stub**. الحل: `Func<HttpRequestMessage, HttpResponseMessage>`. نفس درس ملكية الـ `IDisposable` بتاع TASK-006 من الناحية التانية.

**🔵 Nits:** `Assert.DoesNotContain("INTERNAL", ...)` ملوش قدرة يفشل بعد الـ `Assert.Equal` · الـ `HttpClient`/handler مش بيتعملهم dispose في التيستات · سبب اختيار `FakeLogger` مكتبش (الـ ticket طالبه) · **الـ stub بيتجاهل الـ `HttpRequestMessage` تماماً** — صفر تحقق من الـ URL/method/`x-goog-api-key`/الـ prompt (بره الـ scope بقصد، بس ده مدخل TASK-009).
- **اللي قبلها:** ✅ TASK-007 اتقفلت — Approved في r2 (2026-08-25). الهجرة شغالة end-to-end (ترجمة حقيقية رجعت من الـ Interactions API)، 0 warnings، format نضيف، ومفيش `Unkown` في أي ملف. الكوميت `fa6c556`.

### 🎫 TASK-008 — الملخص

**الشكل:** مشروع xUnit جديد + stub لـ `HttpMessageHandler` + **5 تيستات على `GeminiApiService`** (نجاح · `model_output` مش أول step · 429 + `RetryAfter` · JSON بايظ · status مجهول) + **بند إجباري: يكسر سطرين ويلصق الـ output الأحمر**.

**الدافع (مسجّل بالتفصيل في الـ ticket):**
1. **نمط "بيكتب دفاع مش قادر يشتغل" اتأكد 3 مرات** (TASK-005 guard في المكان الغلط · TASK-006 `Validation` من غير `: IValidatableObject` · TASK-007 `Unkown`/`Unknown`). التلاتة عدّوا بصفر warnings. ده التدخّل المقترح في المفكرة، مطبّق.
2. **الـ quota عايق فعلي** — 20/يوم، وخلصت مرتين في review واحد. التاسك دي **بتاكل صفر requests** وبتديله القدرة يخلّي Gemini "يرجّع" أي حاجة عايزها.

**الفخاخ المزروعة (تتراجع في الـ review):**
- 🪤 **تعديل production code عشان التيست يعدّي** — الـ ticket بيقول صراحة "قف واسأل"، والإجابة معروفة: مش محتاج (اتأكدت — كل الـ types المطلوبة `public`).
- 🪤 **تيست #5 يـ assert على القيمة الراجعة بدل اللوج.** بج TASK-007 كان `NotCompleted(Unknown)` **راجع صح تماماً** — البايظ كان التحذير بس. أي assert على الـ outcome هيعدّي أخضر مع نفس البج. لو وقع فيها، التاسك اتحوّلت لطقوس.
- 🪤 **الـ interface.** الـ decision block بيربط بقرار #15: الـ seam هنا طلع `HttpMessageHandler` مش interface → **قرار #15 يفضل قايم**.
- 🪤 **بند (د) اتخطى** — الـ review بيقف من غير قراية الباقي. تيست عمره ما اتشاف أحمر = نفس بج الـ guard اللي المسار مبيعدّيش عليه.

**السؤال بالتجربة:** الفرق بين body = نص عادي و body = JSON مقصوص — هل الاتنين بيوصلوا لنفس الـ `catch (JsonException)`؟ (التلميح: بصّ على الـ `Content-Type` اللي هو نفسه بيبنيه في الـ stub).

### ⬜ مفتوح بعد TASK-007

1. ✅ ~~**قرار #18 معلّق**~~ — **اتقفل 2026-08-27 بدليل من الداتابيز بتاعته.** الرد جه بالـ alias `gemini-flash-latest` زي ما بعتناه بالظبط → القرار **اتقلب**، و**تثبيت اسم الموديل اتنقل من M8 لأقرب فرصة**. التفاصيل في جدول القرارات.
2. **🔴 الـ billing عايق فعلي** — اللوج لسه بيقول `free_tier, limit: 20`. اتأكد عملياً في الجلسة دي: الـ quota خلصت **مرتين** أثناء المراجعة لوحدها. أي تاسك جاية بتلمس الـ AI هتقف على ده. الترقية لـ Tier 1 = ربط billing account وبتشتغل فوراً، وسقفها $250.
3. **⬜ دَين الـ 422 (الحجب)** — مالهاش مصدر موثّق في الـ Interactions API. الـ `errors[]` بتتسجّل خام لحد ما نشوف حالة حقيقية.
4. **⬜ شكل `errors[]`** — مجهول لحد أول فشل.
5. **⬜ س1/س2 والسؤالين** — مجاوبش عليهم (الجلسة كانت طويلة جداً). **مايتحسبوش تخطّي** — الشغل نفسه اتحقق منه بالتشغيل.

**✅ اتحل ضمنياً بمجرد نجاح الرد:** `model` **string** مش object (وإلا كان `JsonException` → 502)، و`steps` فيها فعلاً `model_output` بـ `content[]` نص (وإلا `ExtractText` كانت رجّعت null).
- **اللي قبلها:** ✅ TASK-006 اتقفلت — Approved في r2 (2026-08-25) مع دين مؤجل بقراره (تحت)
- **الكوميت:** `0224a54` (الجسم الأساسي) + تعديلات r2 (`using (httpResponse)` + `SystemTextJsonValidationMetadataProvider`)
- **✅ متحقق عملياً (2026-08-25):** `GET /v1/languages` → 200 + `Cache-Control: public,max-age=3600` · `banana` → 400 بمفتاح `sourceLanguage` · `""` → 400 · `en → en` → **400** · **صفر** استدعاءات Gemini في كل الحالات · build 0 warnings · `dotnet format` نضيف.
- ~~**⏸️ وقوف:** مفيش TASK-007 لحد ما هو يطلب.~~ — اتقفل، TASK-007 اتعملت واتقفلت.

### 💳 دين معلوم — مؤجل بقرار صريح منه (2026-08-25)

> قال **"مش عايز اعمل التعديلات الباقية"** بعد ما الـ blockers الاتنين اتقفلوا. **دي مش بنود متخطّاة** — دي تأجيل واعي بعد ما اتعرضت عليه بتكلفتها. ماتتحسبش عليه في مفكرة الأنماط.

| # | البند | المكان | التكلفة الحقيقية |
|---|---|---|---|
| 1 | مفتاح الخطأ `TargetLanguage` بالـ PascalCase | `TranslationRequest.cs:27` | `nameof` بيدي اسم الـ C#، والـ `SystemTextJsonValidationMetadataProvider` **مبيمسّش** الـ `memberNames` اللي بتتسلّم يدوي في `IValidatableObject`. النتيجة: نفس الـ endpoint بيرجّع `sourceLanguage` camelCase و `TargetLanguage` PascalCase. الحل: نص صريح `["targetLanguage"]` |
| 2 | typo `"differnt"` | `TranslationRequest.cs:26` | ظاهر في response حقيقي |
| 3 | تعليق `// 504:` مقصوص | `TranslationsController.cs:54` | داخلي |
| 4 | `[AttributeUsage(AttributeTargets.Property)]` ناقصة | `SupportedLanguageAttribute.cs` | داخلي |
| 5 | سطر فاضي زيادة بعد `using (httpResponse) {` | `GeminiApiService.cs:86` | تجميل |

**البند 1 هو الوحيد اللي ليه أثر على الـ contract** — يترجعله مع M4 (توحيد شكل الأخطاء) على أبعد تقدير.

**🔎 اكتشاف تقني يتسجّل:** الـ `SystemTextJsonValidationMetadataProvider` بيصلّح مفاتيح الأخطاء الجاية من `ValidationAttribute` **بس**. الأخطاء الجاية من `IValidatableObject` بتتسلّم مفتاحها يدوي فبتعدّي من غير mapping. وكمان: الـ constructor الفاضي بتاعه **بيحط CamelCase ثابتة جواه**، مش بيقرا `JsonSerializerOptions.PropertyNamingPolicy` — يعني قرار التسمية مكتوب في مكانين مستقلين ومحدش هيحذّرك لو اتفرّقوا.

### 🆕 اكتشاف كبير منه (2026-08-25) — Gemini Interactions API

راح قرا توثيق Google من نفسه ولقى إن **`generateContent` مبقتش المستحسنة**: الـ **Interactions API** بقت GA و Google بتقول بالنص إنها الـ recommended standard primitive لأي مشروع جديد. **المعلومة دي كانت غايبة عني** — اتأكدت منها وهو صح.

| القديم | الجديد |
|---|---|
| `POST v1beta/models/{model}:generateContent` بـ `{contents:[...]}` | `POST v1beta/interactions` بـ `{model, input}` |
| `candidates[0].content.parts[]` | `steps[].content[]` (جوه step نوعه `model_output`) |
| `finishReason` | `status` على الـ step |
| `modelVersion` | **مش موجود** |
| `usageMetadata.promptTokenCount` | `usage.prompt_tokens` (snake_case) |

⚠️ **breaking changes مايو 2026:** الـ `outputs` array اتشال لصالح `steps`، والـ legacy schema اتحذف 8 يونيو 2026 — يعني أي مثال قديم على النت بايظ.
📎 [migrate-to-interactions](https://ai.google.dev/gemini-api/docs/migrate-to-interactions) · [interactions-overview](https://ai.google.dev/gemini-api/docs/interactions-overview) · [breaking-changes-may-2026](https://ai.google.dev/gemini-api/docs/interactions-breaking-changes-may-2026)

**اتفصلت عن TASK-006 بقرار مني** (كانت هتخلي التاسكين غير قابلين للمراجعة) → بقت **TASK-007**. هو رجّع الملف نضيف من غير جدال.

**🔴 قرار #18 اتكسر بسببها:** مفيش `modelVersion` في الرد الجديد، يعني حقل `model` في `TranslationResponse` مالوش مصدر. القرار لازم **يتفتح تاني في TASK-007**، ولو الإجابة بقت `_options.Model` يبقى **تثبيت اسم الموديل** (المؤجل لـ M8) بقى ألزم مش أقل.
- **الوقت المتاح أسبوعياً:** ⏳ في انتظار الرد

### الشغال دلوقتي

- `GET /health` → `status` / `time` / `environment`
- `POST /v1/translations` → **ترجمة حقيقية من Gemini** بـ **تصنيف كامل للفشل** (TASK-005): `TranslationOutcome` hierarchy → 503 (quota) · 500 (طلبنا/الـ key مرفوض) · 504 (timeout) · 502 (المزوّد) · 422 (SAFETY/RECITATION) · 500 (MaxTokens/Other/Unknown). الـ key والـ Model بيتحققوا عند الـ startup (`ValidateOnStart`)، Timeout = 15 ثانية في `AddHttpClient`، ومفيش أي `ex.Message` بيوصل للعميل.

### أسئلة مفتوحة مؤجلة لـ M8

- **HTTPS:** `UseHttpsRedirection` اتشالت خالص (قرار صح — الـ 307 على POST مش مناسب لـ mobile clients، والـ TLS بيتفك عند الـ edge). البديل: `UseHsts` + إجبار https عند الـ reverse proxy.
- **الدومين:** لسه متحددش. `api.transly.ai` في كود الفلاتر **placeholder** مش قرار.
- **الـ path prefix:** `/api` ممكن تتضاف عند الـ proxy (`UsePathBase`) من غير كود لو الدومين طلع من غير subdomain.

## التاسكات

| ID | العنوان | Milestone | الحالة | نتيجة الـ review |
|---|---|---|---|---|
| TASK-011 | الكاش ميقدّمش ترجمة من موديل تاني — تثبيت الموديل + مصدر واحد لحقل `Model` + إبطال الكاش | M3 | ✅ Done (Approved) | **الكود اتقفل نضيف من أول تسليم بعد المراجعة الأولى** — تثبيت الموديل، `ModelName` على `GeminiApiService`، index مركّب `(CacheKey, Model)`، migration مطابقة للمتوقع (`DropIndex`+`CreateIndex` بس، مفيش تعديل عمود). build 0 warnings، format نضيف، test 6/6. **الإثبات العملي جه صح من أول مرة:** 3 صفوف، 2 موديل مختلفين، MISS ثم HIT بعد تغيير الموديل — نفس نمط "بيتحقق بالتشغيل مش بالقراية" اللي اتسجّل في TASK-010. **سؤال الـ index order** جاوب "مش بيفرق" وده صح لمنع التكرار بس ناقص نص الصورة (leftmost prefix لاستعلام مستقبلي بـ `CacheKey` لوحده) — اتشرحله. **سؤال `cached.Model` vs `model`** "مش عارف" بصراحة — اتشرحله إنهم متطابقين مضمون من الـ `WHERE` والفرق تجميلي بس |
| TASK-001 | تنضيف الـ template + `/health` + أول commit | M0 | ✅ Done | Approved في الـ round التالت — اتنين rounds اتضاعوا في acceptance criteria متقروش |
| TASK-002 | `POST /v1/translations` بـ records و fake logic | M2 | ✅ Done | Approved في r3. الـ contract مطابق حرف بحرف، `required`+`init` على الكل، 400 ProblemDetails مجاناً. الـ rounds الزيادة كانت ملاحظات متطبقتش مش أخطاء كود |
| TASK-003 | الـ AI proxy الحقيقي — Gemini + typed HttpClient + user secrets | AI proxy | ✅ Done | Approved في r4. الـ boundary اتقفل صح في الآخر، والـ error handling اتاختبر على الحقيقي (503 + 429 من Gemini) وعدّى. الـ rounds التلاتة الأولى كلها كانت **نفس الدرس** بتلات أشكال — مين المسؤول عن معرفة ايه |
| TASK-004 | `TranslationTone` enum + switch expression + توحيد شكل الخطأ | M2 | ✅ Done | Approved في r2. الشكل العام صح من أول مرة (`_ => throw` مش `_ => ""`، 0 warnings، global converter بـ CamelCase). الـ r1 كانت ثغرة واحدة: `allowIntegerValues` فضل `true` → `"tone": 99` كان بيرجّع 500 + stack trace. اتصلحت واتحققت (99 → 400 ✅، 1 → 400 ✅) |
| TASK-010 | أول EF Core — كاش الترجمات في MySQL: entity + DbContext + migration + المسار | M3 | ✅ **Done (Approved r2، الأربعة اتقفلوا r3)** | **r3 اتحقق بنفسي 2026-08-27:** `Design` بقت 10.0.9 · build **0 warnings / 0 errors** · `dotnet format` نضيف · `dotnet test` **6/6** (التيستات عدّت رغم إن `TranslationOutcome.Success` اتغيّر تحتها — لأنها بتـ assert على النوع والقيم مش بتبني الـ record). | **الشغل نفسه صح**: الـ migration مطابق للمتوقع حرف بحرف (`varchar(64)`+unique index · `Tone` بقت `varchar(16)` مش int · `longtext` · `datetime(6)`)، والـ `SELECT` أثبت مسار الكتابة بترجمة ar→en حقيقية بنبرة Casual، والميكروثانية (`.726522`) اتحفظت. **⚠️ ملحوظة على التقييم: معظم الكود اتسلّم له في رسالة "ساعدني"** — اللي كان قراره فعلاً: مكان الملفات (اختار صح) والتشغيل والتحقق. **r1 كان فيها 3 blockers ردّ عليهم كلهم بنيّة معلنة قبل الكوميت** (connection string، Timeout، الإثبات) — مقبولة، نفس منطق حجته المقبولة في TASK-003. **وأهم مخرج من التاسك مجاش من الكود — جه من الـ `SELECT`:** قرار #18 اتقفل واتقلب |
| TASK-009 | سدّ حدود `TranslateAsync` + الـ stub يقرا الـ request + أول `[Theory]` | M7 | ❌ **اتلغت بطلبه** (2026-08-27) | "مش مقتنع بالتاسك دي · قم بتبديلها". اتبدّلت من غير تفاوض. **الخطأ كان مني**: M7 اتقدّمت لسبب محدد، اتحقق في TASK-008، وكان لازم أرجع للـ curriculum فوراً بدل ما أكمّل بالعطالة. الشغل الفني اللي كان فيها اتسجّل كدين (تسريب الـ charset) وموعده M4 |
| TASK-008 | أول tests — xUnit + stub لـ `HttpMessageHandler` + 5 تيستات على `GeminiApiService` + إثبات إنها بتفشل | M7 | ✅ Done (r1 + تأجيل بقراره) | **الفخّين الأساسيين عدّاهم**: صفر تعديل في `TranslyAI.Api/`، وتيست #5 بيـ assert على اللوج مش على القيمة الراجعة. وكتب تيست سادس مكنش مطلوب (الصف التاني من جدول السؤال اتحوّل لـ regression guard). 6/6 أخضر، 0 warnings، format نضيف. **الـ blockers**: (1) بند (د) مفيش منه أثر — صفر output أحمر، وchecklist التسليم فاضية (تخطّي acceptance criteria للمرة الخامسة)، (2) الصف التالت من جدول السؤال متجرّبش — وهو الوحيد اللي بيكشف إن exception بتطلع برّه `TranslateAsync` |
| TASK-007 | هجرة الـ AI proxy لـ Gemini Interactions API + إعادة اشتقاق تصنيف الفشل | AI proxy | ✅ Done (Approved r2) | **التاسك اللي هو جابها.** الهيكل كان صح من r1 كله — DTOs، status enum، `ExtractText` بالنوع مش بالموضع، جدول الـ outcome. الـ blockers التلاتة في r1: (1) `Unkown` **و** `Unknown` في نفس الـ enum → التحذير كود ميت بـ 0 warnings، (2) الـ RAW dump اتساب فبيسجّل نص المستخدم كامل وبيلغي قرار `store: false` اللي هو نفسه أخده، (3) الـ 422 معلّق عليها — **دي طلعت مقصودة منه واتسحبت من التصنيف**. اتصلحوا في r2 وترجمة حقيقية عدّت |
| TASK-006 | `GET /v1/languages` + الكتالوج مصدر وحيد + رفض اللغات غير المدعومة بـ 400 | M2 | ✅ Done (Approved r2) | r1 كان فيها blocker حقيقي: `IValidatableObject` مش معلنة والميثود اسمها `Validation` → الـ `en → en` check **كود ميت** بـ 0 warnings، واتثبت بطلب حقيقي راح لـ Gemini. والـ `Dispose` اتعمل على `_httpClient` (مستعار) بدل `HttpResponseMessage` (مملوك). الاتنين اتصلحوا في r2 واتحققوا عملياً. الجسم الأساسي كان صح من أول مرة — الكتالوج والـ endpoint والـ attribute والـ caching headers كلهم اشتغلوا |
| TASK-005 | الـ AI proxy يفشل بصدق — finishReason + تفرقة أنواع الفشل + ValidateOnStart + Timeout | AI proxy | ✅ Done (Approved r2) | الشكل المعماري صح: الـ hierarchy مظبوطة، **وعدّى الفخ الأساسي** (503 مش 429) + مرّر `Retry-After` + فرّق timeout عن client-cancel بـ exception filters. الـ blockers: (1) `catch (Exception)` بيرجّع `ex.Message` للعميل وبيعمل mapping موازي للـ hierarchy، (2) الفخ المزروع اتغطى غلط — الـ guard بتاع `candidates` بيمسك `[]` مش الحقل الناقص، (3) الـ acceptance criterion بتاع تعليق "ليه الـ status ده" اتخطى بالكامل + `dotnet format` متشغلش |

## قرارات معمارية اتاخدت

| # | القرار | اخترنا | البديل المرفوض | السبب باختصار |
|---|---|---|---|---|
| 1 | شكل الـ solution | مشروع واحد (`TranslyAI.Api`) | 4 projects: Api/Application/Domain/Infrastructure | الـ API لسه endpoint أو اتنين — الـ layering دلوقتي ceremony مش testability. نقسّم لما يبقى فيه سبب حقيقي |
| 2 | API style | Controllers | Minimal APIs لكل حاجة | الـ API رايح على auth + subscriptions. بس `/health` فضل `MapGet` لأنه infrastructure مش domain |
| 3 | مكان الـ Gemini key | على السيرفر بس | obfuscation أو تشفير الـ key في الـ APK | أي key بيوصل للـ device بيتسرب — decompile أو HTTPS proxy. مفيش حل client-side |
| 4 | نوع الوقت | `DateTimeOffset` | `DateTime` | `DateTime.Kind` بيضيع على أول boundary (DB/serialization). الـ offset جوه القيمة في `DateTimeOffset` |
| 5 | شكل حقل `status` | string (`Healthy`/`Degraded`/`Unhealthy`) | `bool ok` | الـ bool مبيعرفش يعبر عن Degraded، وتغيير الـ contract بعدين مكلف على mobile clients |
| 6 | نوع الـ DTOs | `record` | `class` | immutability + value equality + `with` من غير codegen — الـ Freezed بتاعته بس جوه اللغة. الـ `class` للـ entities اللي ليها identity |
| 7 | الـ route والـ status | `POST /v1/translations` بـ 200 | `/translate` (فعل)، `/api/v1/...`، أو 201 + Location | **الـ resource:** feature الـ history هتتنقل للسيرفر → `GET /v1/translations` نفس الـ resource، والفعل مبيديش الشكل ده. **الـ prefix:** قرار رخيص وقابل للرجوع — `/api` ممكن تتضاف في الـ reverse proxy وقت النشر (`UsePathBase`) من غير كود. الـ `/v1` هو اللي مش رخيص، لأنه اللي بيسمح بكسر الـ contract من غير كسر التليفونات في السوق. **الـ status:** 201 من غير resource محفوظة تبقى كذبة — الـ Location هيرجع 404 |
| 8 | شكل الـ DTO | مسطّح — `sourceLanguage: "en"` | نسخ `LanguagePair` المتداخلة بكل حقول `Language` | السيرفر محتاج الكود بس؛ `name`/`isRtl` حاجات عرض. شكل الـ wire ≠ شكل الـ entity |
| 9 | المحرّك في الـ response | `model: "stub"` → بعدين اسم الموديل | نسخ `enum { mlKit, ai }` | السيرفر عمره ما هيشغّل ML Kit. والـ string بيقول **أي** موديل ترجم — يفرق في debugging وتغيير الموديلات. الـ client بيعمل map لـ `TranslationEngine.ai` في الـ data layer |
| 10 | `id` في الـ response | **مفيش دلوقتي** | `id` مولّد، أو `int` زي `HistoryEntry` | مفيش persistence فالـ id كذبة. إضافة حقل بعدين مش breaking. ولما ييجي يبقى `string` — الـ int التسلسلي بيسرّب حجم الاستخدام وبيسمح بالعد |
| 11 | الـ envelope | مفيش — الـ object مباشرة | `{ "data": {...} }` | HTTP فيه status codes و`ProblemDetails`. الـ envelope بس لما يكون فيه metadata جنب البيانات (pagination) |
| 12 | الـ quota | headers (`X-RateLimit-Remaining`) بعدين، مش في الـ body | حقل في الـ translation response | الـ quota مش جزء من الترجمة — في الـ headers بتشتغل على كل الـ endpoints بنفس الشكل |
| 13 | ازاي ننادي HTTP | `IHttpClientFactory` + typed client بـ `HttpClient` خام | `new HttpClient()` · `static HttpClient` · Gemini SDK | الـ `new` كل request = socket exhaustion (`TIME_WAIT`)، والـ `static` بيكاش الـ DNS للأبد. الـ factory بيعمل rotate للـ handlers كل دقيقتين. والـ SDK هيتاخد بس لو احتجنا streaming/function calling/token counting |
| 14 | مكان الـ API key | user secrets في الـ dev، env vars في الـ prod (M8) | `appsettings.Development.json` + gitignore · Key Vault دلوقتي | user secrets بره الريبو خالص (`%APPDATA%\Microsoft\UserSecrets`) — مستحيل يتكوميت. الـ `appsettings.Development.json` **أصلاً متكوميت** في الريبو ده. حماية بتعتمد على الانضباط مش حماية |
| 15 | `ITranslationService` | مفيش interface — الـ controller بياخد الـ class نفسه | interface + implementation واحدة | الـ interface بتكسب seam بس. مفيش tests ولا مزوّد تاني ولا decorator → صفر مكسب. تتضاف أول ما واحدة من التلاتة دول تحصل — والـ extract refactor ثانيتين. في الفلاتر الـ abstraction دي كانت في محلها لأن `TranslationRepositoryImpl` بيختار runtime بين ML Kit والـ AI؛ هنا مفيش اختيار |
| 16 | إشارة الفشل من الـ service | `return null` (الـ signature `Task<GeminiTranslationResult?>`) | `throw` exceptions مخصصة | caller واحد وstatus code واحد (502) → الـ `null` مش بيضيّع معلومة. **يتحوّل لـ exceptions** أول ما نحتاج نفرّق 429 (quota) عن 502 (المزوّد واقع) عن 500 (key غلط) — وساعتها الـ mapping يروح لـ `IExceptionHandler` مركزي في M4 |
| 17 | شكل `GeminiTranslationResult` | `string Text` و `string ModelVersion` **مش nullable** | `string?` + فحص في الـ controller | النوع اللي بيسمح بـ `Text == null` بيمثّل حاجة ملهاش معنى، وبيجبر **كل** متصل يدافع عن نفسه. الفشل بيتقال مرة واحدة عن طريق `null` من الـ method كلها |
| 18 | حقل `model` في الـ response | ~~`modelVersion` الراجع من Gemini~~ → ✅ **اتقلب 2026-08-27: `_options.Model` باسم موديل صريح مثبّت** | `response.Model` (بقى صدى بلا قيمة) | **اتقفل بدليل من الداتابيز بتاعته.** الصف الأول في `CachedTranslations` جه فيه `Model = gemini-flash-latest` — **نفس الـ alias اللي بعتناه**. يعني الـ Interactions API **بترجّع الـ string زي ما استلمته**، مش الاسم المحلول (ده كان سلوك `generateContent` القديم واتأكد وقتها). الحقل بقى بيقول اللي إحنا قلناه = صفر معلومة. **والكاش خلّى ده دايم:** كل صف مكتوب فيه الـ alias، فلما يتحرك تحتينا هيبقى عندنا جدول فيه ترجمات من موديلين مالهمش علامة تفرّقهم ومفيش إشارة للإبطال. **النتيجة: "تثبيت اسم الموديل" اتنقل من M8 لأقرب فرصة** — مادة خام، ماتتحولش لتاسك إلا بطلب منه |
| 19 | نوع حقل `tone` | `enum` + `JsonStringEnumConverter` | `string` + regex/constants/`if` | الفرق مش validation — الفرق مين بيتحمّل المسؤولية. الـ string بيخلي **كل** method تسأل "هي دي قيمة صالحة؟"؛ الـ enum بيسأل مرة واحدة عند الـ binder والباقي بيشتغل على قيمة مضمونة. نفس مبدأ قرار #17. الـ string بيبقى صح بس لو القيم بتيجي من config/DB ومش معروفة وقت الـ compile |
| 20 | الـ `_` arm في الـ switch expression | `_` بيرمي exception | من غير `_` (نعيش مع CS8509) · `_ => ""` | **enums في C# مش exhaustive زي Dart** — تحتها `int` و`(TranslationTone)99` بيـ compile. الـ binder بيحرس المدخلات الخارجية؛ الـ `_` بيحرس من الكود نفسه (cast غلط، أو عضو enum جديد بعد سنة من غير تعليمة). الـ `_ => ""` هي نفس الـ bug لابسة نوع جديد |

| 21 | فين الـ transport exceptions بتتحول لـ outcome | ✅ **اتطبق في r2** — جوه `GeminiApiService` | `try/catch` في الـ controller (اللي عمله) · `IExceptionHandler` مركزي دلوقتي | الـ return type بتاع `TranslateAsync` بيقول "أنا بقولك كل اللي ممكن يحصل". لو الـ method برضه بترمي، النوع بيكذب والـ compiler مش قادر يساعد. الـ `HttpRequestException`/`TaskCanceledException` بيتولدوا جوه الـ service — يتحولوا لـ vocabulary الـ outcome في نفس المكان. التكلفة لو فضلوا في الـ controller: في M4 بـ 5 endpoints الـ try/catch ده هيتنسخ 5 مرات |
| 23 | الـ MySQL provider | `MySql.EntityFrameworkCore` 10.0.9 (Oracle) | `Pomelo.EntityFrameworkCore.MySql` | **Pomelo واقفة عند 9.0.0 — مفيش نسخة لـ EF Core 10** والمشروع `net10.0` و`dotnet ef` 10.0.9 (اتأكدت من nuget). الفخ إن Pomelo هي الأشهر وهي اللي في كل الـ tutorials، فأول بحث هيوديه ناحيتها ويحطه في معركة نسخ. التكلفة المقبولة: provider أقل شهرة = إجابات أقل على الإنترنت — **وده سبب إضافي يقرا الـ SQL المولّد بنفسه**. يترجع للقرار ده لو Pomelo نزّلت EF 10 |
| 24 | الكاش قبل الـ history | كاش على (نص + زوج + نبرة) من غير مالك | `GET /v1/translations` history لكل مستخدم | الـ history محتاج يجاوب "الترجمة دي بتاعة مين؟" ومفيش إجابة قبل الـ auth (M5). الكاش **مالوش مالك أصلاً** — نفس النص من أي حد بيدي نفس الناتج — فبيشتغل دلوقتي بالظبط. وبيحل عايق قايم (الـ 20/يوم) بدل ما يستنى حاجة تانية |
| 22 | `required` على DTO جاي من مزوّد خارجي | ✅ **اتطبق في r2** — كل الحقول nullable، والـ enum بيتبني من `string?` بـ `MapFinishReason` مع `Unknown` | `required` على كل حقل · `JsonStringEnumConverter` مباشرة على الـ enum | `required` + System.Text.Json = `JsonException` لما الحقل ينقص. ده صح للـ **مدخلات بتاعتنا** (الـ binder بيحوّلها 400 ProblemDetails)، وغلط للـ **ردود المزوّد** — الحقل الناقص هناك مش خطأ عميل، دي حالة معروفة (`promptFeedback.blockReason`) والـ exception بتخبّيها |

## 🛑 قاعدة ثابتة — مفيش تاسك من غير طلب صريح

> **اتفرضت 2026-08-27 بعد خرق مباشر.** الطلب الأصلي اتقال في 2026-08-20 (**"بعد ما اخلص هذه التاسك لا تعطيني تاسك تانية!!!"**) واتحُرم وقتها. في جلسة 2026-08-27 اتخرق **مرتين** بعد "انتهيت": مرة بوعد بتاسك جاية، ومرة بكتابة ticket كامل وتسليمه.

**القاعدة:**

1. **"خلصت" / "انتهيت" = طلب review. مش طلب تاسك.**
2. بعد ما الـ review يخلص → **حدّث `progress.md` وقف**. الرد بينتهي عند نتيجة الـ review.
3. التاسك الجديدة **متتكتبش ومتتسلّمش** غير لما يطلبها بالكلام ("اديني تاسك"، "الخطوة الجاية"، "عايز تاسك جديدة").
4. **ولا حتى تلميح** — "هديك التاسك الجاية"، "التاسك الجاية هتكون كذا"، "بعد ما تكوميت هديك..." كلها خرق للقاعدة. الـ review بينتهي بنتيجته، مش بوعد.
5. الاستثناء الوحيد: لو **هو** سأل "إيه الخطوة الجاية؟".

**ليه القاعدة دي مش شكلية:** التاسك اللي بتتفرض عليه بتحوّل العلاقة من "أنا بطلب شغل" لـ "الشغل بيتساق عليّ". وده بالظبط اللي خلّى TASK-009 تترفض — اتكتبت بالعطالة مش بطلب.

**التصحيح اللي اتعمل:** الـ ticket اللي اتكتب في الخرق **اتمسح بالكامل بطلبه** — الملف والصف في الجدول وكل إشارة ليه. مفيش تاسك محضّرة مقدماً، ولو طلب تاسك بتتكتب ساعتها بعد ما يتحدد معاه موضوعها.

## 🔒 قاعدة ثابتة — Definition of Submitted

> اتفرضت بعد TASK-002 round 1. قبل ما يقول "خلصت" لازم يعدّي التلاتة دول:
> 1. يفتح آخر رسالة review ويعدّ الملاحظات — كل واحدة يا اتصلحت يا ليها سبب معلن
> 2. يفتح ملف التاسك ويعدّ الـ acceptance criteria
> 3. `dotnet format` + `git status`
>
> لو بعت شغل وفيه ملاحظة قديمة متطبقتش من غير سبب → الـ review بيرجع من غير ما أقرا الباقي.

## مفكرة الـ mentor — نقاط بتتكرر

> لما حاجة تتكرر 3 مرات، تتحول لتاسك مخصصة ليها.

- 🔴 **بيبعت الشغل للمراجعة من غير ما يطبّق ملاحظات الـ review السابقة — 3 مرات، اتفعّلت.**
  - TASK-001 r1 → `status` ناقص
  - TASK-001 r2 → نفس `status` لسه ناقص بعد ما اتقال صريح
  - TASK-002 r1 → تلات ملاحظات في رسالة واحدة (`DateTimeOffset` / namespace / `record` mutable)، طبّق واحدة وبعت "خلصت"
  - **التدخّل:** قاعدة "Definition of Submitted" فوق بدل تاسك مخصصة — المشكلة process مش معرفة. هو عارف `DateTimeOffset`، بس مش بيرجع للرسالة.
  - **✅ تحسّن في TASK-003:** طبّق الملاحظات كلها round بعد round، ولما ساب واحدة **قال ليه** (الـ `ApiKey` في `appsettings.json`) — وده بالظبط اللي القاعدة طالباه. القاعدة شغالة، تفضل.
- 🔴 **بيتخطى acceptance criteria مكتوبة صراحة — تكرار رابع (TASK-005 r1).** الـ ticket طالب "جنب كل status code اكتب سطر تعليق بيقول ليه هو ده" وقال بالنص "لو مفيش سبب، يبقى الاختيار غلط". النتيجة: **صفر تعليقات** في الـ controller. وكمان `dotnet format` متشغلش (بند 3 في Definition of Submitted) — `--verify-no-changes` طلّع ~25 خطأ whitespace في نفس الملف.
  - **الفرق عن المرات اللي فاتت:** المرات اللي قبلها كانت ملاحظات review سابقة. دي **acceptance criteria في ملف التاسك نفسه** — يعني المشكلة مش "بينسى يرجع للرسالة"، المشكلة إنه **مبيفتحش الـ ticket تاني قبل ما يقول خلصت**.
  - **الـ criterion ده مكنش شكلي** — التعليق كان الـ mechanism اللي بيه أعرف اختار ولا خمّن. وغيابه بالظبط هو اللي خلّى `InvalidRequest → 500` (اللي بيغطي 400 و403 مع بعض) عدّي من غير ما حد ياخد باله.
  - **التدخّل المقترح لو اتكرر خامس مرة:** الـ submission ميتقبلش من غير checklist متعبّي — بند بند، وجنب كل واحد لينك للسطر في الكود.
- 🔴🔴 **بيكتب دفاع/تشخيص مش قادر يشتغل — التكرار التالت اتأكد (TASK-007). اتفعّل: تاسك مخصصة مستحقة.**
  1. **TASK-005:** guard على `candidates` والمسار مبيعدّيش عليه أصلاً (`JsonException` بترمي قبله).
  2. **TASK-006:** ميثود `Validation` من غير `: IValidatableObject` → الـ framework عمره ما نداها.
  3. **TASK-007:** الـ enum فيه `Unkown` **و** `Unknown` مع بعض. `MapStatus` بترجّع `Unknown`، والفحص بيقارن بـ `Unkown` → **التحذير عمره ما هيطلع**.
  - **القاسم المشترك:** الكود **بيـ compile بصفر warnings** في التلات حالات، لأن كل حالة منهم "صحيحة" نحوياً. الـ compiler مش بيسأل "هل ده هيتنفذ؟".
  - **دي مش غفلة — دي منهج:** بيكتب الدفاع وبيعتبر الكتابة هي التنفيذ. الناقص خطوة واحدة: **اجعل الحالة تحصل، وشوف الكود اشتغل**.
  - **✅ التدخّل اتطبّق (2026-08-26) — TASK-008.** M7 اتقدّمت من مكانها. مش عشان "coverage"، عشان الـ test هو **الآلة اللي بتجبرك تشغّل المسار**. الحالات التلاتة دي كان اختبار واحد هيمسك كل واحدة فيهم.
  - **البند الحاسم في التاسك = (د): يكسر سطر ويشوف التيست أحمر.** تيست عمره ما اتشاف أحمر هو **نفس** الـ bug: كود بيدّعي إنه بيحرس من غير ما حد يثبت إنه بيشتغل. لو عدّى البند ده صح، النمط اتعالج. لو تخطّاه، النمط اتنسخ في مكان جديد.
  - **🔁 النتيجة في r1 (2026-08-27): البند مالوش أثر في التسليم.** قال بعد الـ review **"جربتها بنفسي خلاص"** — مقبول كنية، بس **شكل الأحمر هو المعلومة مش وجوده**: أنهي سطر اتكسر، وأنهي تيستات وقعت وأنهي تيستات فضلت خضرا. كسر سطر بيوقّع الستة = صفر معلومة عن عزل التيستات؛ كسر `"model_output"` وتوقيع #1 و#2 **بس** = دليل إن كل تيست بيقيس حاجة مختلفة. الطلب اتعاد بالصيغة دي.
- 🟢 **~~"بيقول عملتها بدل ما يوريها"~~ — التصنيف ده اتسحب في نفس الجلسة (2026-08-27).** سجّلته كنمط لما رد "جربتها بنفسي خلاص"، وبعدين أكّد **"انا جربت كل البتقولو والشغل سليم"**. حسب السابقة المسجّلة في دين TASK-006، التأكيد الصريح بعد عرض التكلفة = **تأجيل واعي مش تخطّي** — وماينفعش يتحسب في المفكرة. اتشال.
  - **اللي فضل هو ملاحظة تعليمية مش نمط:** لما نطلب أثر لتجربة، المطلوب **شكل** النتيجة مش وجودها — أنهي سطر اتكسر وأنهي تيستات وقعت وأنهي فضلت خضرا. الأخير هو اللي بيثبت إن التيستات معزولة.
  - **الدرس للـ mentor (تكرار لدرس TASK-007):** لما ترد على "الشغل سليم" بإصرار على الإجراء، انت بتحوّل النقاش عن الموضوع. **الحل: حوّل البند لشغل حقيقي في التاسك الجاية** — وده اللي اتعمل، بند (ج) في TASK-009 فيه أحمر بيحصل من نفسه من غير تمرين مصطنع.
- 🟡 **بيحط guard في المكان الغلط ويفتكر إنه غطى الحالة — جديدة في TASK-005.** كتب `if (response is null || candidate is null)` وهو فاكر إنها بتغطي "Gemini رجّع 200 من غير candidates". اتحقق عملياً: الحالة دي بترمي `JsonException` جوه `ReadFromJsonAsync` **قبل** ما الـ guard يشتغل أصلاً. الـ guard بيمسك `"candidates": []` بس — حالة تانية خالص.
  - **مرتبط بنمط قديم:** "بيعتمد على الـ default من غير ما يفتح الـ signature". هنا الشكل الجديد: **بيكتب دفاع من غير ما يتأكد إن المسار بيعدّي عليه**. الـ ticket كان طالب صراحة "مش هقولك الإجابة، جرّبها" — والتجربة مأتمّتش.
- 🔴 **بيخلط بين "أملك" و"مستعير" في الـ `IDisposable` — جديدة في TASK-006 r1.** الملاحظة كانت: `HttpResponseMessage` جوه `TranslateAsync` مش بيتعمله dispose. اللي عمله: `GeminiApiService : IDisposable` + `_httpClient.Dispose()`. يعني **عمل dispose لحاجة الـ DI أداهاله، وساب الحاجة اللي هو عملها.**
  - **الأثر الحقيقي:** `_httpClient.Dispose()` مش بيعمل حاجة أصلاً (الـ factory بتلف الـ handler في `LifetimeTrackingHttpMessageHandler` والـ `Dispose` بتاعه no-op) — فهو مش خطر، هو **صفر**. والـ `HttpResponseMessage` لسه مسايب. وكمان `: IDisposable` بقى ادعاء عام في الـ type مش وراه حاجة.
  - **القاعدة اللي المفروض تفضل:** dispose اللي **انت** عملته، مش اللي **اتداهولك**.
  - **الأخطر:** الكود الكامل للحل كان **متبعتله حرفياً** في رسالة "ساعدني" (`using (httpResponse) { ... }`) — يعني المشكلة مش معرفة، دي إنه اشتغل من فهمه للعنوان مش من نص الملاحظة.
  - مرتبط بنمط "بيحط guard في المكان الغلط" — نفس الشكل: **إجراء صح على هدف غلط**.
- 🔴 **`DateTime` بدل `DateTimeOffset` — 3 مرات** (TASK-001 r1، TASK-002 r1، وبعد ما اتنبّه في رسالة الـ unblock). القرار #4.
- 🟡 **بيحل مشكلة الـ compiler بدل مشكلة التصميم — جديدة في TASK-003.** طلعله `CS8604` فخلّى الـ record `string?` عشان الـ warning يسكت، بدل ما يضمن القيمة. **✅ متكررش في TASK-004** — كتب `_ => throw` مش `_ => ""` رغم إن التانية كانت أسهل وكانت هتسكّت `CS8509`. النمط ده اتحسن، يفضل مراقب من بعيد.
- 🟡 **بيعتمد على الـ default بتاع API من غير ما يفتح الـ signature — جديدة في TASK-004.** كتب `new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)` من غير ما ياخد باله إن فيه parameter تاني `allowIntegerValues` الافتراضي بتاعه `true`. النتيجة: ثغرة 500 + stack trace من مدخل خارجي. **والتاسك كانت طالبة منه صراحة يجرّب `"tone": 1`** وقال إنها هي اللي هتحدد الإجابة — وتخطّاها.
  - **مرتبط بنمط قديم:** "بيتّبع الـ snippets حرفياً من غير ما يسأل بتعمل ايه". الفرق إن هنا الـ snippet كان من دماغه، فالمشكلة أعمق: **الثقة في الـ default من غير قراية**.
  - **التدخّل:** كل تاسك من هنا ورايح فيها "سؤال تجاوب عليه بالتجربة" — سؤال واحد لازم يجاوب عليه برد فعلي مش بافتراض. اتحط في TASK-005.
  - لو اتكرر بعد التدخل ده → تاسك مخصصة: يقرا signature كامل لكل API جديد قبل ما يستعمله.
- 🟢 **~~تكرار رابع لعادة الـ comment~~ — التصنيف ده اتسحب (TASK-007 r1).** أنا صنّفت الـ 422 المعلّق عليها كتكرار رابع لعادة "التعليق بدل الحذف". **وهو صحّح: التعليق كان مقصود** — عايز فاصل بصري قدامه يفكّره إن لازم يبقى فيه طريقة نعرف بيها إن النص اتحجب. النية دي **سليمة** ومختلفة تماماً عن التلات مرات اللي فاتت (تردد في الحذف). الاتنين شكلهم واحد في الـ diff، فالتصنيف من غير ما أسأله كان تسرّع مني.
  - **الملاحظة الباقية على المكانيكية مش على النية:** الكود المعلّق عليه بينادي `GeminiFinishReason.Safety` و`notCompleted.FinishReason` — **الاتنين اتمسحوا في نفس الـ commit**. يعني مش ممكن يتشال منه التعليق ويشتغل؛ هو **أحفورة** مش كود مؤجل. والتذكير اللي هو عايزه **موجود أصلاً** في التعليق اللي تحته مباشرة.
  - **الدرس للـ mentor:** لما شكل الـ diff يحتمل نيّتين، **اسأل قبل ما تسجّل نمط**. تسجيل نمط غلط في المفكرة أغلى من سؤال.
- 🟡 **بيعمل comment على الكود بدل ما يمسحه — تكرار تاني (TASK-005 r2).** الأولى: comment على `EnsureSuccessStatusCode()` عشان يوقف الـ 500، فخلّى الفشل يعدّي صامت — الغريزة "أشيل اللي بيزعق" بدل "أتصرف في اللي بيزعق منه". التانية: `// [JsonConverter(typeof(JsonStringEnumConverter))]` في `GeminiResponseDto` سابها comment بدل ما يمسحها.
  - **الفرق بين المرتين:** الأولى كانت خطر حقيقي (فشل صامت)، التانية نص ميّت مش أكتر. بس **الغريزة واحدة** — التردد في الحذف.
  - **الرد:** الـ git هو الـ history. سطر متعلّق عليه comment من غير تاريخ ولا سبب بيخلي اللي بعدك يقف يفكر "ده اتشال ليه؟ ينفع يرجع؟" — والإجابة موجودة في `git log` أنضف مليون مرة.
  - لو اتكرر تالت مرة → تاسك مخصصة.
- بيتّبع الـ snippets الجاهزة حرفياً من غير ما يسأل هي بتعمل ايه (GitHub's "create a new repository on the command line" → commit فيه README بس)
- بيصلّح الملاحظة في مكان واحد ويسيب المكان التاني المطابق (صلّح `TranslationRequest` وساب `TranslationResponse`)
- ميل واضح لنقل Clean Architecture من الـ Flutter كما هي (عمل 3 فولدرات layers قبل ما يكتب endpoint واحد) — استجاب للـ pushback من غير جدال
- مبيشغّلش `dotnet build --no-incremental` ومبيبصش على الـ Problems panel — warnings كانت واقفة وهو شايف "Build succeeded"

## نقاط قوة

- Senior Flutter — DI، layering، async، REST، JSON، auth flows، CI/CD كلها مفاهيم مفهومة. السينتاكس بس الجديد.
- بيسمع للـ pushback لما يكون معاه سبب، مش نبرة. مسح الـ 3 فولدرات من غير جدال.
- بيسأل أسئلة صح قبل ما ينفذ ("الـ status يكون فيها ايه؟") بدل ما يخمن.
- Conventional Commits من نفسه من غير ما حد يطلبها.
- بيسأل قبل ما ينفذ لما يشك في قرار ("`/api/v1` ولا `/v1`؟"، "مين قال إني عايز `api.transly.ai`؟") — والتانية دي كانت اعتراض في محله على استنتاج مبني على placeholder.
- استوعب `required` + `init` من كلمتين مفتاحيتين من غير كود. **ملاحظة لـ M4:** `required` + `[ApiController]` بيدوا 400 + ProblemDetails مجاناً — نبني عليها بدل ما نبدأ من الصفر.
- عنده تمارين .NET قديمة في `source/repos` (HR.LeaveManagement, BookStoreApp, MyFirstApi) — لسه محتاجين نعرف وصل فيها لفين
- **بيعترض بحجة لما يكون معاه حق (TASK-003).** رفض يشيل `ApiKey` من `appsettings.json` وقال السبب: توثيق شكل الـ configuration + مفيش حد تاني على المشروع. حجة سليمة واتقبلت. **درس للـ mentor:** متعلّقش ملاحظة process كبيرة على أضعف نقطة في الليستة — ده بيحوّل النقاش عن الموضوع.
- **بيوصل للحل الصح لو الشرح فيه "ليه" مش "ايه".** في TASK-003 محتاج 4 rounds، بس كل round كان بيتحرك خطوة حقيقية لما السبب اتشرح بمثال عملي (سيناريو 429) بدل قاعدة مجردة.
- **🔺 عدّى الفخ المقصود في TASK-005 من غير تلميح.** الـ ticket زرع إغراء صريح يرجّع **429** على نفاد الـ Gemini quota وحذّر منه بفقرة كاملة — واختار **503 + `Retry-After`** ومرّر الـ `Delta` الجاي من Gemini. ده أهم اختبار في التاسك وعدّاه. كمان فرّق الـ timeout بتاعنا (504) عن قطع العميل (499) بـ **exception filters** (`when`) — ميكانيكية C# مش بديهية لواحد جاي من Dart، ومحدش قاله عليها.
- **🔺🔺 تحققه العملي أنتج المعلومة اللي قفلت قرار معلّق — المرة التانية (2026-08-27).** لصق صف واحد من `SELECT` عشان يثبت إن الكاش شغال، والصف ده هو اللي **قفل قرار #18** (الـ `Model` جه بالـ alias مش بالاسم المحلول) — حاجة كانت معلّقة من TASK-007 وكنت بستناها منه كسؤال. **الدرس:** أهم مخرج من التاسك مجاش من الكود، جه من إنه بص على البيانات الحقيقية. **يتقال له صراحة** — ده بيربط "أثبت اللي عملته" بمكسب ملموس بدل ما تفضل إجراء.
  - **ملحوظة على التقييم:** معظم كود TASK-010 اتسلّم له في رسالة "ساعدني". اللي كان قراره فعلاً: مكان الملفات (اختار صح) + التشغيل + التحقق. **متتحسبش التاسك دي كدليل على قدرته يكتب EF Core من الصفر** — ده يتقاس في تاسك جاية.
- **🔺🔺 راح لتوثيق المزوّد من نفسه ولقى حاجة الـ mentor مكنش يعرفها (2026-08-25).** اكتشف إن `generateContent` مبقتش المستحسنة وإن الـ Interactions API بقت GA — من غير ما حد يوجهه، وغيّر الكود على أساسها. **الاكتشاف صح واتأكد.** ده أعلى مستوى وصله لحد دلوقتي: مش بيقرا الـ ticket بعين ناقدة بس، بقى بيتحقق من **الافتراضات اللي الـ ticket نفسه مبني عليها**.
  - **اللي كان ناقص:** غيّر الـ request وساب الـ response — والنتيجة كانت هتبقى **502 على كل ترجمة ناجحة في صمت**. الاكتشاف ممتاز، الهجرة نصها. الدرس: **تغيير الـ API مش تغيير endpoint — هو تغيير الـ contract بالكامل، الاتجاهين.**
  - **واستجاب للفصل من غير جدال** لما اتقاله إن ده TASK-007 مش TASK-006.
- **🔺 بيقرا الـ ticket بعين ناقدة قبل ما ينفّذ — تصاعد واضح (2026-08-20).** على TASK-005 اعترض على حاجتين قبل ما يكتب سطر: (1) الجزء أ "تفاصيل غير مهمة" — **صح جزئياً**، تثبيت الموديل اتشال من التاسك؛ (2) **"مش هنعمل Global Error Handler في M4؟ ليه نفترض إنه مش موجود ونعمل hierarchy؟"** — دي لقطة حقيقية، الـ decision block كان عدّد البدائل من غير ما يحسب إن `IExceptionHandler` جاي قريب. اتضاف قسم رد كامل في الـ ticket.
  - **الفرق عن الاعتراضات اللي قبلها:** الأولانيين (`/api/v1`، `api.transly.ai`) كانوا أسئلة عن معلومة ناقصة. ده اعتراض على **الـ sequencing المعماري** — بيقارن التاسك بخريطة الـ milestones ويسأل عن الازدواج. ده تفكير tech lead مش junior.
  - **درس للـ mentor:** تسمية "تسخين" على بنود الجزء أ هي اللي خلّتهم يبانوا زي التنضيف. **التسمية جزء من الـ ticket** — لو البند مدخل للشغل الأساسي، اسمه ميقولش إنه جانبي.

## أسئلة intake لسه مجاوبش عليها

1. ✅ ~~فين ريبو الفلاتر؟~~ — `mohammed-xp/transly_ai`، اتقرا والـ contract اتستخرج
2. مستوى الـ C#: LINQ و EF Core migrations عملهم بإيده قبل كده ولا لأ؟
3. التطبيق فيه users/Firebase Auth دلوقتي؟ (بيحدد شكل M5) — مفيش feature auth في الريبو، فالأغلب لأ
4. الوقت الأسبوعي بالساعات
5. ✅ ~~Gemini ولا Claude؟~~ — **Gemini**، والـ key جاهز عنده (2026-08-16)

## 🗺️ خريطة Transly — milestones مش تاسكات

> الخريطة العامة M0→M8 في `.claude/skills/dotnet-mentor/references/curriculum.md`.
> الجدول ده بيزرع فيها الحاجات الخاصة بـ Transly اللي مش موجودة هناك.
> **خريطة مش عقد** — الترتيب بيتغير حسب المشروع، والتاسكات بتتكتب واحدة واحدة بعد كل review.

| | Milestone | الحالة | خاص بـ Transly |
|---|---|---|---|
| M0 | Setup | ✅ | — |
| M1 | C# لمطور Dart | 🔄 بيتاخد جوه التاسكات | `record`/`init`/`required`/`DateTimeOffset` اتاخدوا في TASK-002 |
| M2 | أول endpoints | 🚧 **هنا** | `POST /v1/translations` ✅ · `GET /v1/languages` ⬜ · `TranslationTone` enum ⬜ |
| — | **الـ AI proxy** | ✅ | اتعمل في TASK-003: `AddHttpClient<GeminiApiService>` + `GeminiOptions` من user secrets + prompt بالنبرات التلاتة + 502 على فشل المزوّد. **الباقي منه (= TASK-005):** `finishReason` + تفرقة أنواع الفشل، `ValidateOnStart` للـ key، Timeout. تثبيت اسم الموديل اتنقل لـ M8 |
| M3 | EF Core | 🚧 **هنا** | TASK-010 ✅: **كاش الترجمات** — نفس النص+الزوج+النبرة مايتشتراش مرتين. **TASK-011 ✅:** الكاش يعرف بأي موديل اتولد كل صف + تثبيت اسم الموديل + حقل `Model` من مصدر واحد. الـ history مؤجّل لأنه محتاج identity (M5) |
| M4 | Validation / errors / logging | ⬜ | شكل الخطأ يطابق `sealed class Failure` عند الـ client (Network/Server/Cache/Offline/Unknown) |
| M5 | Auth (JWT) | ⬜ | مفيش auth في التطبيق دلوقتي — تصميم من الصفر على الجهتين |
| — | **العدّاد والاشتراكات** | ⬜ | **السبب اللي اتبنى عشانه الباك اند.** عدّ الاستهلاك (حروف ولا requests؟ — راجع درس `char`/`Rune`)، خطط، quota في `X-RateLimit-*` headers، رفض 429 |
| — | **Streaming (SSE)** | ⬜ | الترجمة تظهر تدريجياً بدل انتظار الرد كامل — مكسب حقيقي في UX لتطبيق ترجمة |
| M6 | Production concerns | ⬜ | caching للترجمات المتكررة (نفس النص + نفس الزوج = نفس الناتج — توفير مباشر في فاتورة الـ AI) |
| M7 | Testing | ⏸️ **اتقدّمت، سببها اتحقق في TASK-008، ورجعنا للـ curriculum** | TASK-008 ✅: الـ seam هو `HttpMessageHandler` مش interface (قرار #15 قايم). TASK-009 🚧: سدّ الحدود اللي بتسرّب exception + الـ stub يقرا الـ request (الـ prompt/tone/`store:false`). `WebApplicationFactory` (outcome → status code) اتأجلت لـ M4 مع الـ error contract |
| M8 | Docker + CI + نشر | ⬜ | الدومين، HTTPS عند الـ edge، توجيه `ApiEndpoints.baseUrl` على المنشور، و**تثبيت اسم الموديل** بدل `gemini-flash-latest` (اتنقلت من TASK-005 — قرار #18) |

## الجلسة الجاية

> **الحالة بتاريخ 2026-08-30 (آخر الجلسة):** TASK-008 ✅ · TASK-009 ❌ اتلغت · TASK-010 ✅ اتكوميتت (`4e893d7`) · **TASK-011 ✅ اتقفلت واتكوميتت (`ab4c389`)**.
>
> ## 🛑 وقوف — مفيش تاسك لحد ما يطلب
>
> **ابدأ الجلسة الجاية من غير ما تفتح موضوع تاسك خالص.** القاعدة الثابتة تحت.
>
> **حاجة واحدة بس فضلت من TASK-011 — ملحوظة تصحيح صغيرة اتقالتله، مش بلوكر:** الـ ticket كان فيه بند "دوّر على مكان تاني بيستخدم `response.Model`" — ركّز بس على السطر اللي بيرجّع `Success`. المكان التاني ([GeminiApiService.cs:140](../TranslyAI.Api/Services/GeminiApiService.cs#L140)) الـ guard `string.IsNullOrWhiteSpace(response.Model)` **لسه موجود** بعد التعديل. اتفحص في الـ review ومكانش بلوكر لأن الحقل لسه بيتملى (الـ API بترجّع echo)، بس **دلوقتي بقى ميت منطقياً** — لو Gemini يوماً رجّع الحقل فاضي (سلوك مش متضمّن)، ترجمة ناجحة تترفض بـ 502 بسبب حقل مالوش قارئ حقيقي. **مش عايق، بس مادة لأول تاسك تلمس `GeminiApiService` تاني.**
>
> **⬜ لسه من TASK-010:** `"Hello"` vs `"hello"` (درس الـ collation)، وعدد الـ SQL statements في المرة التانية.
>
> **⚠️ ملحوظة على الـ build:** الـ app بتاعه بيفضل شغال وبيقفل ملفات `bin/` — الـ build بيقع بـ MSB3027. الحل من غير ما تقفل عليه عمليته: `dotnet build -p:BaseOutputPath=<مسار في الـ scratchpad>`.
>
> **أسئلة تجريبية من TASK-010 لسه مجاوبش عليها:** `"Hello"` vs `"hello"` (نداء واحد ولا اتنين — درس الـ collation)، وعدد الـ SQL statements في المرة التانية.
>
> **⚠️ درس الجلسة دي — يتقرا قبل كتابة أي ticket جاي:** M7 اتقدّمت بقرار لسبب محدد (نمط متكرر 3 مرات)، السبب اتحقق في TASK-008، وأنا كتبت TASK-009 في نفس الموضوع بالعطالة. **رفضها، وكان محقاً.** الإشارة كانت في رسالته الأولى ("خلصت تاسك 8 و**عايز تاسك جديدة**"). القاعدة: milestone بتتقدّم لسبب → أول ما السبب يتحقق، ارجع لمسار الـ curriculum.
>
> **أسئلة معلّقة لسه محتاجة إجابة منه:**
> - 🔴 **قرار #18** — قيمة حقل `model` من رد ناجح: `gemini-flash-latest` (alias) ولا الاسم المحلول؟ سؤال واحد بيقفلها. لو alias → تثبيت اسم الموديل بينتقل من M8 لدلوقتي.
> - 🔴 **الـ billing** — الترقية لـ Tier 1 عملها ولا لأ؟ عايق على أي تاسك بتلمس Gemini.
> - ⬜ الوقت المتاح أسبوعياً (سؤال intake رقم 4، لسه مجاوبش).

---

### أرشيف — TASK-005

**✅ TASK-005 اتقفلت — Approved في r2 (2026-08-24).** التلات blockers اتصلحوا كلهم، والفخ المزروع اتحقق منه بتجربة فعلية (5 حالات JSON، صفر exceptions).

**✅ اتكوميت:** `5b5ab5c fix: distinguish AI proxy failure modes and stop leaking internals (TASK-005 r1-r2)`. الـ working tree نضيف.

**🟡 ملاحظات r2 — حالتها بعد الكوميت:**
- ✅ `// [JsonConverter(...)]` المعلّق في `GeminiResponseDto` **اتمسح**.
- ⬜ التعليقات في الـ controller لسه محتفظة بالـ "إيه" وضايع منها الـ "مش إيه": `// 503: الـ quota خلصت` من غير "مش 429 لأن الـ 429 محجوزة لـ M5". النص المفقود ده هو **الوحيد** اللي بيمنع حد يرجّعها 429 بعد سنة. **يترجعله وقت M5** لما الـ 429 تتحجز فعلاً.
- ⬜ `// 504:` اتقصّت في النص وسايبة فاصلة في آخرها.
- ⬜ `HttpResponseMessage` مش بيتعمله dispose (كان في الـ snippet بتاعي كمان — غلطتي). يتصلح مع أول تاسك تلمس `GeminiApiService`.

**❓ مش متحقق منه:** هل شغّل `dotnet run` + طلب 200 بعد تعديلات r2؟ الـ `GeminiOptions` اتمست (شيل `= string.Empty`) فمسار الـ startup اتغيّر بعد آخر تحقق فعلي.

**✅ إجابات الجزء ج اتقبلت (2026-08-24):**
- **س1:** الافتراضي **100 ثانية**، حطها **15**، والنوع `TaskCanceledException`. والجزء التاني من السؤال (ليه النوع ده مشكلة وانت ماسك `CancellationToken`) **جاوبه في الكود مش بالكلام** — `when (!cancellationToken.IsCancellationRequested)` عشان يفرّق الـ timeout بتاعنا عن إن العميل هو اللي قطع. إجابة صح.
- **س2 (دين TASK-004 — اتقفل):** الـ response بيرجّع **`"casual"`** بالـ camelCase. يعني الـ `JsonStringEnumConverter(CamelCase)` شغال على الخرج، **والـ parse عند تطبيق الفلاتر مش هيكسر**. الدين ده اتشال.

---

**الـ ticket الأصلي** — `mentor/tasks/TASK-005.md`، اتكتب 2026-08-19.

**محتوى TASK-005 باختصار:** تفعيل قرار #16 — الـ `null` الواحد بيتحوّل لـ `sealed record` hierarchy بيفرّق بين quota / key غلط / المزوّد واقع / `finishReason != STOP`، وكل حالة ليها status code. جنبها: `ValidateOnStart` للـ key + Timeout صريح.

**تعديل 2026-08-20 بعد اعتراضه:** ~~تثبيت اسم الموديل~~ اتشالت من التاسك ونقلت لـ M8 — حجته إنها production checklist مش شغل دلوقتي، وهي كده فعلاً (قرار #18 مسجّلها كـ "قبل الإنتاج" أصلاً). واتضاف للـ ticket قسم **"بس إحنا هنعمل Global Error Handler في M4 — ليه الـ hierarchy؟"** يرد على اعتراضه التاني.

**الفخ المزروع في التاسك (يتراجع في الـ review):**
- **429 مش الإجابة للـ quota** — لو اتحرقت دلوقتي، مش هيبقى فيه كود يقول "**انت** خلصت رصيدك" في M5. التاسك بتحذّر منه صراحة، فلو اختارها برضه يبقى مقراش.
- **`GeminiResponseDto.Candidates` معرّفة `required`** — و Gemini بيرجّع 200 من غير `candidates` لما الـ prompt يتحجب → `JsonException` غير ممسوكة → 500. اتزرع كسؤال مش كتعليمة.

**✅ دين التحقق بتاع TASK-004 اتقفل** — `"casual"` camelCase، شوف قسم "إجابات الجزء ج" فوق.

**⚠️ عايق مطروح عليه في التاسك:** الـ free tier 20/يوم مش كفاية لتجربة TASK-005 (200 + عدة حالات فشل). اتطلب منه يقرر الـ paid tier قبل ما يبدأ الجزء ب.

## ✅ الوقوف المقصود اتقفل (2026-08-24)

الطلب الصريح بتاع 2026-08-20 (**"بعد ما اخلص هذه التاسك لا تعطيني تاسك تانية!!!"**) اتحترم — TASK-006 اتكتبت **بعد ما هو طلبها** في جلسة 2026-08-24.

---

## 🎫 TASK-006 — الملخص

**الشكل:** `GET /v1/languages` + كتالوج لغات كـ **مصدر وحيد** + `POST /v1/translations` بيرفض أي كود بره الكتالوج بـ 400 **قبل** ما يكلّم Gemini. الملف: `mentor/tasks/TASK-006.md`.

**الدافع الحقيقي (مش "أول collection response"):** الحقلين `sourceLanguage`/`targetLanguage` دلوقتي **strings حرة بتتحقن مباشرة في prompt رايح لـ LLM** (`GeminiApiService.cs:42`). تلات ثغرات مع بعض: (1) `"banana"` بيتبعت ويتخصم من quota الـ 20/يوم ويرجّع 200 بكلام فاضي، (2) `""` بيعدّي لأن `required` = "الحقل موجود" مش "فيه قيمة"، (3) prompt injection حرفي في الحقل.

**الفخاخ المزروعة (تتراجع في الـ review):**
- 🪤 **الـ envelope على collection.** التاسك بتذكّره بقرار #11 ("مفيش envelope") **من غير ما تقول هل ينطبق هنا**. لو رجّع `[...]` bare بحجة "القرار قال كده" — يبقى بيطبق قرارات ميكانيكياً مش بيفكر فيها. الإجابة المطلوبة هي **السبب**، أياً كان الاختيار.
- 🪤 **`[ResponseCache]` مش بيعمل caching.** س2 بتطلب منه يضرب الـ endpoint مرتين ويتأكد بنفسه هل الـ action اشتغلت تاني. الـ attribute بيكتب headers بس؛ الـ server-side caching محتاج middleware/output cache. **ده بالظبط نمطه المسجّل**: الثقة في الـ API من غير ما يفتحه.
- 🪤 **`ILanguageCatalog`.** التاسك بتحذّره بقرار #15 صراحة. لو عملها برضه لازم يسمّي الـ seam.

**الديون المطوية جواها:** `HttpResponseMessage` dispose (`using var` مش هيمشي على السطر زي ما هو — درس `IDisposable`)، والتعليق المقصوص `// 504:`.

**قرارات مفتوحة سايبها ليه بوعي:** حقول الـ `Language` DTO (باختبار "إيه اللي يكسر لو الـ client hardcode-هُ")، و`en → en` (400 ولا 200؟).

**🆕 تدخّل جديد:** ضفت قسم **"شكل التسليم"** — checklist جاهزة يبعتها متعبّية. ده التدخّل المقترح في المفكرة (بند تخطّي الـ acceptance criteria، 4 مرات)، بس **مطبّق كـ scaffolding مش كعقاب** — قبل التكرار الخامس مش بعده.

> ⚠️ **قبل M3:** لازم نتكلم في الـ Gemini billing. الـ 20 request/يوم مش كفاية للتطوير نفسه (شوف قسم ملاحظات TASK-003). TASK-006 بتاكل من الـ quota أقل من اللي فاتت (معظم التحقق بيقف عند الـ 400 قبل Gemini) — بس مش صفر.

## ملاحظات من TASK-004 تتبني عليها بعدين

- **الـ `_` arm في switch expression على enum بيبقى unreachable من الـ wire بس لو الـ binder رافض الأرقام.** الاتنين لازم يتظبطوا مع بعض — الـ type لوحده مش ضمانة عند الـ HTTP boundary.
- **الـ 400 الافتراضي من `[ApiController]` بيحط رسالة الـ `JsonException` كما هي في الـ ModelState** وفيها الـ FQN بتاع النوع الداخلي. مادة جاهزة لدرس M4 عن الـ error contract.
- في الـ 400 بتاع deserialization فاشل، الـ ModelState بيرجّع كمان `"translation": ["The translation field is required."]` — **اسم الـ parameter في الـ action** بيتسرّب للـ client كأنه اسم حقل. سبب إضافي لـ `IExceptionHandler` مركزي في M4.

## ملاحظات من TASK-003 تتبني عليها بعدين

- **⚠️ تصحيح (2026-08-19): الـ free tier بتاع Gemini = 20 request/*اليوم*، مش الدقيقة.** الـ quota خلصت أثناء review TASK-004 والـ log قال بالنص: `quotaId: GenerateRequestsPerDayPerProjectPerModel-FreeTier` · `quotaValue: 20` · `model: gemini-3.7-flash`. **الأثر:** الـ free tier مش كفاية حتى للـ manual testing اليومي — يعني موضوع الـ billing/paid tier بقى عايق للتطوير نفسه، مش بس feature مؤجلة. يتناقش قبل M3.
- `gemini-flash-latest` alias بيتحل لـ `gemini-3.7-flash` النهاردة. **يتثبت قبل الإنتاج.**
- Gemini بيرجّع `finishReason` (`SAFETY` / `MAX_TOKENS` / `RECITATION`) وإحنا مش عاملينه deserialize — ده اللي هيفسّر الترجمات الفاضية.
- `[ApiController]` + `required` بيدوا `ProblemDetails` كاملة بأسماء الحقول الناقصة مجاناً — اتأكد عملياً. نبني عليها في M4 بدل ما نبدأ من الصفر.

**درس اتزرع ولسه هيتحصد:** `char` في C# = UTF-16 code unit مش حرف. اتبيّن عملياً في stub الترجمة (الإيموجي بقى `��` بعد `Array.Reverse`). نرجعله لما نحسب استهلاك الحروف للـ quota — `StringInfo` / `Rune`.
