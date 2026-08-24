# .NET Mentor — Progress

> ملف الحالة بتاع الـ mentor. بيتقرا في أول كل جلسة وبيتحدث في آخرها.
> مكانه: `mentor/progress.md` في الريبو بتاع الباك اند.

**آخر تحديث:** 2026-08-24

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
| Database | ⏳ PostgreSQL (مبدئي — يتأكد في M3) | مجاني، بيشتغل في Docker، وأرخص hosting من SQL Server |
| ORM | EF Core | الـ default في الـ ecosystem، وبيدي code-first migrations |
| Auth | ⏳ JWT (التفاصيل في M5) | متوقف على إذا التطبيق فيه Firebase Auth ولا لأ |
| Testing | xUnit + NSubstitute | الـ default في .NET |
| Deployment | ⏳ Docker + GitHub Actions (M8) | يتحدد الـ host وقتها |

## الوضع الحالي

- **Milestone:** AI proxy hardening (بين M2 و M4)
- **التاسك المفتوحة:** ✅ **TASK-005 اتقفلت — Approved في r2 (2026-08-24).** التلات blockers اتصلحوا واتحققوا.
- **⏸️ وقوف مقصود:** بناءً على طلبه الصريح، مفيش TASK-006 لحد ما هو يطلب.
- **الكوميت:** `fafe894` (r1) + تعديلات r2 **لسه متكوميتش**.
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
| TASK-001 | تنضيف الـ template + `/health` + أول commit | M0 | ✅ Done | Approved في الـ round التالت — اتنين rounds اتضاعوا في acceptance criteria متقروش |
| TASK-002 | `POST /v1/translations` بـ records و fake logic | M2 | ✅ Done | Approved في r3. الـ contract مطابق حرف بحرف، `required`+`init` على الكل، 400 ProblemDetails مجاناً. الـ rounds الزيادة كانت ملاحظات متطبقتش مش أخطاء كود |
| TASK-003 | الـ AI proxy الحقيقي — Gemini + typed HttpClient + user secrets | AI proxy | ✅ Done | Approved في r4. الـ boundary اتقفل صح في الآخر، والـ error handling اتاختبر على الحقيقي (503 + 429 من Gemini) وعدّى. الـ rounds التلاتة الأولى كلها كانت **نفس الدرس** بتلات أشكال — مين المسؤول عن معرفة ايه |
| TASK-004 | `TranslationTone` enum + switch expression + توحيد شكل الخطأ | M2 | ✅ Done | Approved في r2. الشكل العام صح من أول مرة (`_ => throw` مش `_ => ""`، 0 warnings، global converter بـ CamelCase). الـ r1 كانت ثغرة واحدة: `allowIntegerValues` فضل `true` → `"tone": 99` كان بيرجّع 500 + stack trace. اتصلحت واتحققت (99 → 400 ✅، 1 → 400 ✅) |
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
| 18 | حقل `model` في الـ response | `modelVersion` الراجع من Gemini | `_options.Model` من الـ configuration | اتأكد عملياً: الـ config فيها `gemini-flash-latest` والرد جه `gemini-3.7-flash`. الـ alias بيتحرك تحتيك من غير deploy — الجودة والفاتورة يتغيروا وانت مش فاهم. **قبل الإنتاج: ثبّت اسم موديل صريح** |
| 19 | نوع حقل `tone` | `enum` + `JsonStringEnumConverter` | `string` + regex/constants/`if` | الفرق مش validation — الفرق مين بيتحمّل المسؤولية. الـ string بيخلي **كل** method تسأل "هي دي قيمة صالحة؟"؛ الـ enum بيسأل مرة واحدة عند الـ binder والباقي بيشتغل على قيمة مضمونة. نفس مبدأ قرار #17. الـ string بيبقى صح بس لو القيم بتيجي من config/DB ومش معروفة وقت الـ compile |
| 20 | الـ `_` arm في الـ switch expression | `_` بيرمي exception | من غير `_` (نعيش مع CS8509) · `_ => ""` | **enums في C# مش exhaustive زي Dart** — تحتها `int` و`(TranslationTone)99` بيـ compile. الـ binder بيحرس المدخلات الخارجية؛ الـ `_` بيحرس من الكود نفسه (cast غلط، أو عضو enum جديد بعد سنة من غير تعليمة). الـ `_ => ""` هي نفس الـ bug لابسة نوع جديد |

| 21 | فين الـ transport exceptions بتتحول لـ outcome | ✅ **اتطبق في r2** — جوه `GeminiApiService` | `try/catch` في الـ controller (اللي عمله) · `IExceptionHandler` مركزي دلوقتي | الـ return type بتاع `TranslateAsync` بيقول "أنا بقولك كل اللي ممكن يحصل". لو الـ method برضه بترمي، النوع بيكذب والـ compiler مش قادر يساعد. الـ `HttpRequestException`/`TaskCanceledException` بيتولدوا جوه الـ service — يتحولوا لـ vocabulary الـ outcome في نفس المكان. التكلفة لو فضلوا في الـ controller: في M4 بـ 5 endpoints الـ try/catch ده هيتنسخ 5 مرات |
| 22 | `required` على DTO جاي من مزوّد خارجي | ✅ **اتطبق في r2** — كل الحقول nullable، والـ enum بيتبني من `string?` بـ `MapFinishReason` مع `Unknown` | `required` على كل حقل · `JsonStringEnumConverter` مباشرة على الـ enum | `required` + System.Text.Json = `JsonException` لما الحقل ينقص. ده صح للـ **مدخلات بتاعتنا** (الـ binder بيحوّلها 400 ProblemDetails)، وغلط للـ **ردود المزوّد** — الحقل الناقص هناك مش خطأ عميل، دي حالة معروفة (`promptFeedback.blockReason`) والـ exception بتخبّيها |

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
- 🟡 **بيحط guard في المكان الغلط ويفتكر إنه غطى الحالة — جديدة في TASK-005.** كتب `if (response is null || candidate is null)` وهو فاكر إنها بتغطي "Gemini رجّع 200 من غير candidates". اتحقق عملياً: الحالة دي بترمي `JsonException` جوه `ReadFromJsonAsync` **قبل** ما الـ guard يشتغل أصلاً. الـ guard بيمسك `"candidates": []` بس — حالة تانية خالص.
  - **مرتبط بنمط قديم:** "بيعتمد على الـ default من غير ما يفتح الـ signature". هنا الشكل الجديد: **بيكتب دفاع من غير ما يتأكد إن المسار بيعدّي عليه**. الـ ticket كان طالب صراحة "مش هقولك الإجابة، جرّبها" — والتجربة مأتمّتش.
- 🔴 **`DateTime` بدل `DateTimeOffset` — 3 مرات** (TASK-001 r1، TASK-002 r1، وبعد ما اتنبّه في رسالة الـ unblock). القرار #4.
- 🟡 **بيحل مشكلة الـ compiler بدل مشكلة التصميم — جديدة في TASK-003.** طلعله `CS8604` فخلّى الـ record `string?` عشان الـ warning يسكت، بدل ما يضمن القيمة. **✅ متكررش في TASK-004** — كتب `_ => throw` مش `_ => ""` رغم إن التانية كانت أسهل وكانت هتسكّت `CS8509`. النمط ده اتحسن، يفضل مراقب من بعيد.
- 🟡 **بيعتمد على الـ default بتاع API من غير ما يفتح الـ signature — جديدة في TASK-004.** كتب `new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)` من غير ما ياخد باله إن فيه parameter تاني `allowIntegerValues` الافتراضي بتاعه `true`. النتيجة: ثغرة 500 + stack trace من مدخل خارجي. **والتاسك كانت طالبة منه صراحة يجرّب `"tone": 1`** وقال إنها هي اللي هتحدد الإجابة — وتخطّاها.
  - **مرتبط بنمط قديم:** "بيتّبع الـ snippets حرفياً من غير ما يسأل بتعمل ايه". الفرق إن هنا الـ snippet كان من دماغه، فالمشكلة أعمق: **الثقة في الـ default من غير قراية**.
  - **التدخّل:** كل تاسك من هنا ورايح فيها "سؤال تجاوب عليه بالتجربة" — سؤال واحد لازم يجاوب عليه برد فعلي مش بافتراض. اتحط في TASK-005.
  - لو اتكرر بعد التدخل ده → تاسك مخصصة: يقرا signature كامل لكل API جديد قبل ما يستعمله.
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
| M3 | EF Core | ⬜ | حفظ الترجمات + **نقل الـ history** من `history_local_datasource` للسيرفر (`HistoryEntry` فيها `isFavorite`) |
| M4 | Validation / errors / logging | ⬜ | شكل الخطأ يطابق `sealed class Failure` عند الـ client (Network/Server/Cache/Offline/Unknown) |
| M5 | Auth (JWT) | ⬜ | مفيش auth في التطبيق دلوقتي — تصميم من الصفر على الجهتين |
| — | **العدّاد والاشتراكات** | ⬜ | **السبب اللي اتبنى عشانه الباك اند.** عدّ الاستهلاك (حروف ولا requests؟ — راجع درس `char`/`Rune`)، خطط، quota في `X-RateLimit-*` headers، رفض 429 |
| — | **Streaming (SSE)** | ⬜ | الترجمة تظهر تدريجياً بدل انتظار الرد كامل — مكسب حقيقي في UX لتطبيق ترجمة |
| M6 | Production concerns | ⬜ | caching للترجمات المتكررة (نفس النص + نفس الزوج = نفس الناتج — توفير مباشر في فاتورة الـ AI) |
| M7 | Testing | ⬜ | — |
| M8 | Docker + CI + نشر | ⬜ | الدومين، HTTPS عند الـ edge، توجيه `ApiEndpoints.baseUrl` على المنشور، و**تثبيت اسم الموديل** بدل `gemini-flash-latest` (اتنقلت من TASK-005 — قرار #18) |

## الجلسة الجاية

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

## 🛑 طلب صريح منه (2026-08-20)

> **"بعد ما اخلص هذه التاسك لا تعطيني تاسك تانية!!!"**

بعد ما TASK-005 تتقفل — **وقوف**. مفيش TASK-006، مفيش ترشيحات، مفيش "الخطوة الجاية". الليستة تحت تفضل مكتوبة للجلسة اللي **هو** هيفتحها، ومتتعرضش عليه من غير ما يطلب.

**السياق:** الجلسة دي كانت طويلة وشاقة عليه — TASK-005 اتكتبت واتنفذت في نفس الجلسة، وفيها ~12 round من الـ unblock. الإرهاق مفهوم ومشروع.

---

**المرشحين لـ TASK-006 (للرجوع ليها لما هو يطلب):**

1. **`GET /v1/languages`** (~45د) — أول collection response وأول قرار envelope في الـ lists.
2. **M3 — EF Core** — حفظ الترجمات ونقل الـ history من الـ device.

> ⚠️ **قبل M3:** لازم نتكلم في الـ Gemini billing. الـ 20 request/يوم مش كفاية للتطوير نفسه (شوف قسم ملاحظات TASK-003).

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
