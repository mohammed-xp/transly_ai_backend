# .NET Mentor — Progress

> ملف الحالة بتاع الـ mentor. بيتقرا في أول كل جلسة وبيتكتب من جديد في آخرها.
> مكانه: `mentor/progress.md` في الريبو بتاع الباك اند.
> **حالة بس، مش سجل:** حد أقصى ~150 سطر. تفاصيل كل تاسك (الـ ticket، الـ review rounds، الفخاخ، الأسئلة) في `mentor/tasks/TASK-XXX.md`، والقرارات وقواعد العمل في `mentor/decisions.md`.
> الملف بيوصف الحالة عند آخر review، مش حالة الريبو — أي بند مفتوح يتأكد من الكود قبل ما يتقال.

**آخر تحديث:** 2026-09-23 (الملف اتضغط من 944 سطر — كل التفاصيل اتنقلت زي ما هي لملفات التاسكات و`decisions.md`) · **آخر commit اتراجع:** `a226348` (TASK-016)

---

## المشروع

- **المنتج:** Transly AI — تطبيق موبايل لترجمة النصوص بالـ AI، والباك اند ده هو الـ API بتاعه.
- **الـ slice المتفق عليه:** الترجمة end-to-end من خلال الـ API — الـ API هو اللي بيكلم Gemini، والـ key على السيرفر بس. الدافع الحقيقي: الحسابات + الـ quota + الاشتراكات.
- **باك اند موجود قبل كده؟** لأ.
- **العملاء:** تطبيقات موبايل — الـ API مستقل عن أي client. أرشيف الـ contract القديم اللي اتستخرج من الفلاتر في [TASK-002](tasks/TASK-002.md).

## الـ Stack المتفق عليه

| العنصر | الاختيار |
|---|---|
| .NET | 10 (net10.0) |
| API style | Controllers (+ `MapGet` لـ `/health`) |
| AI provider | Gemini — Interactions API ([TASK-007](tasks/TASK-007.md)) |
| Database | MySQL |
| ORM | EF Core + `MySql.EntityFrameworkCore` 10.0.9 (مش Pomelo) |
| Auth | JWT HS256 + جدول `User` بإيدنا + `IPasswordHasher<User>` (قرار #25 و#26) |
| Testing | xUnit + integration tests بـ `WebApplicationFactory` على MySQL حقيقية + `GeminiStubHandler` |
| Deployment | ⏳ Docker + GitHub Actions (M8) — الـ host لسه متحددش |

السبب ورا كل اختيار في [decisions.md](decisions.md).

## الوضع الحالي

- **الاتجاه:** 🚧 **M4 — [TASK-018](tasks/TASK-018.md)** اختياره 2026-09-23 من 3 (CI · M4 · refresh tokens؛ الترشيح كان CI). **M8** لسه الاتجاه الأكبر (طلبه 2026-09-02)، ⬜ متقسّمش ومستني إجابتين: **الـ host** · **الـ Gemini tier** (الـ free tier = 20 request/يوم).
- **التاسك المفتوحة:** [TASK-018](tasks/TASK-018.md) — `ApiResponse<T>` على كل رد. وقت الكتابة كان بدأ الجزء (أ) في الـ working tree (الـ `slnx` + الـ compile).
- **شغل من غير ticket (2026-09-09 → 2026-09-23) — لسه متراجعش:** `a151f48` · `b31a204` · `025d3c4` · `4ccca52` · `ff7fdd2` · `07b472f`. فيه: envelope `ApiResponse<T>` على الردود · interfaces للـ services · AutoMapper · الـ DTOs اتسمّت `*Dto` · الـ routes بقت `api/v1/...` · تعديل في `AuthEndpointTests`.
  - **#7 و#11 اتقلبوا بقراره 2026-09-23** (`api/v1` · envelope للنجاح وProblemDetails للفشل). #27 اتسحب قبل التنفيذ. **#4** بيتصلح في TASK-018. **#15** (الـ interfaces) لسه مفتوح — الجدول في أول [decisions.md](decisions.md).
  - **اتشاف وقت الضغط (قراية، مش review):** [TranslationsController.cs:57](../TranslyAI.Api/Controllers/TranslationsController.cs#L57) بيعمل `return Ok();` والـ `response` اللي اتبنى فوقه مبيترجعش — الترجمة الناجحة بترجع 200 من غير body.
  - **اتأكد بالتشغيل 2026-09-23 (طلب تاسك، مش review):** الـ test project اتشال من `TranslyAI.slnx` في `025d3c4` → `dotnet test` على الـ solution بيشغّل صفر tests. ولوحده مش بيـ compile (`TranslationRequest` في `GeminiApiServiceTests.cs:40`)، والـ integration tests بتضرب `/v1/...` والـ routes بقت `api/v1/...`. ومفيش تيست بيقرا body الترجمة الناجحة — الـ status بس.
  - **`ex.Message` بيطلع للعميل** في الـ 500 بتاع [AuthController.cs:50](../TranslyAI.Api/Controllers/AuthController.cs#L50).
- **التيستات عند آخر review:** 11 (TASK-016). النهاردة مش بتـ compile ومش في الـ solution.

## التاسكات

| ID | العنوان | Milestone | الحالة |
|---|---|---|---|
| TASK-001 | تنضيف الـ template + `/health` + أول commit | M0 | ✅ Approved r3 |
| [TASK-002](tasks/TASK-002.md) | `POST /v1/translations` بـ records و fake logic | M2 | ✅ Approved r3 |
| [TASK-003](tasks/TASK-003.md) | الـ AI proxy — Gemini + typed HttpClient + user secrets | AI proxy | ✅ Approved r4 |
| [TASK-004](tasks/TASK-004.md) | `TranslationTone` enum + switch expression | M2 | ✅ Approved r2 |
| [TASK-005](tasks/TASK-005.md) | الـ proxy يفشل بصدق — تصنيف الفشل + `ValidateOnStart` + Timeout | AI proxy | ✅ Approved r2 |
| [TASK-006](tasks/TASK-006.md) | `GET /v1/languages` + كتالوج اللغات + رفض غير المدعوم بـ 400 | M2 | ✅ Approved r2 + دين بقراره |
| [TASK-007](tasks/TASK-007.md) | الهجرة لـ Gemini Interactions API | AI proxy | ✅ Approved r2 |
| [TASK-008](tasks/TASK-008.md) | أول tests — xUnit + stub لـ `HttpMessageHandler` | M7 | ✅ r1 + تأجيل بقراره |
| [TASK-009](tasks/TASK-009.md) | سدّ حدود `TranslateAsync` | M7 | ❌ اتلغت بطلبه |
| [TASK-010](tasks/TASK-010.md) | أول EF Core — كاش الترجمات في MySQL | M3 | ✅ Approved r2 |
| [TASK-011](tasks/TASK-011.md) | الكاش بيعرف الموديل + تثبيت اسم الموديل | M3 | ✅ Approved |
| [TASK-012](tasks/TASK-012.md) | `User` + register + password hashing | M5 | ✅ Approved r2 |
| [TASK-013](tasks/TASK-013.md) | login + إصدار JWT + `GET /v1/auth/me` | M5 | ✅ Approved r3 |
| [TASK-014](tasks/TASK-014.md) | قفل `/v1/translations` + `TranslationUsages` + أول FK | M5 | ✅ Approved |
| [TASK-015](tasks/TASK-015.md) | أول integration tests — `WebApplicationFactory` + MySQL حقيقية | M7 | ✅ Approved r2 |
| [TASK-016](tasks/TASK-016.md) | quota لكل مستخدم + 429 + `X-RateLimit-*` | العدّاد (M5→M6) | ✅ Approved r2 |
| [TASK-017](tasks/TASK-017.md) | `GET /v1/translations` — history + pagination | M5→M6 | ❌ اتلغت بقراره قبل أي كود |
| [TASK-018](tasks/TASK-018.md) | error contract: `ApiResponse<T>` للنجاح + ProblemDetails للفشل | M4 | 🔁 r2 — 7/14 حمرا (التيستات بتقرا الـ DTO من غير الـ envelope)، ب/ج/د لسه |

**الكود اتسلّم كامل في «ساعدني» في:** TASK-010 · 012 · 013 · 014 · 015 · 016 · 018 (الجزء أ بس) — التقييم فيهم على التشغيل والقرارات اللي غيّرها، مش على الكتابة.

## القرارات المعمارية

في [decisions.md](decisions.md): 27 قرار + قواعد العمل بتاريخها. #27 (exception مخصوصة للفشل المتوقع) اتسحب قبل التنفيذ. اتقلب بقراره 2026-09-23: #7 (`api/v1`) و#11 (envelope للنجاح + ProblemDetails للفشل). ⚠️ #4 و#15 الكود لسه مختلف عنهم.

## خريطة الـ milestones

| | Milestone | الحالة |
|---|---|---|
| M0 | Setup | ✅ |
| M1 | C# لمطور Dart | 🔄 بيتاخد جوه التاسكات |
| M2 | أول endpoints | ✅ translations · languages · tone |
| — | الـ AI proxy | ✅ Interactions API + تصنيف الفشل |
| M3 | EF Core | ✅ TASK-010/011 |
| M4 | Validation / errors / logging | 🚧 **TASK-018** (error contract) — Serilog وcorrelation IDs لسه |
| M5 | Auth | ✅ register · login · `/me` · usage — ⬜ refresh tokens |
| — | العدّاد والاشتراكات | 🚧 الـ quota ✅ (TASK-016) — الخطط والاشتراكات ⬜ |
| — | Streaming (SSE) | ⬜ |
| M6 | Production concerns | ⬜ |
| M7 | Testing | 🔄 TASK-008 · TASK-015 |
| M8 | Docker + CI + نشر | 🎯 **الاتجاه الحالي** — ⬜ متقسّمش |

## مفكرة الـ mentor — نقاط بتتكرر

> سطر لكل نمط، والتفاصيل في ملف التاسك المذكور. لما حاجة تتكرر 3 مرات تتحول لتاسك مخصصة — لما يطلب تاسك.

- 🔴🔴 **بيكتب دفاع مش قادر يشتغل** ×3 (TASK-005/006/007) → اتعالج بـ M7. **✅ اتقفل في TASK-016** — الدفاع مسك الـ blockers بنفسه. اللي فاضل أبسط: **التوقيت** — يشغّل `dotnet test` قبل ما يبعت. → [TASK-008](tasks/TASK-008.md) · [TASK-016](tasks/TASK-016.md)
- 🔴 **بيغيّر جزء من الـ contract ويسيب الباقي** ×3 (TASK-007 · TASK-013 مرتين) — الرابعة → تاسك مخصصة. → [TASK-013](tasks/TASK-013.md)
- 🔴 **بيتخطى acceptance criteria مكتوبة** ×6 (لحد TASK-013 r2) — الـ gate الإجرائي اتسحب، والإثبات بقى تيستات من TASK-015. → [TASK-005](tasks/TASK-005.md) · [TASK-013](tasks/TASK-013.md)
- 🔴 **بيبعت من غير ما يطبّق ملاحظات الـ review السابقة** ×3 (TASK-001/002) → Definition of Submitted، واتحسّن من TASK-003. → [TASK-003](tasks/TASK-003.md)
- 🔴 **`DateTime` بدل `DateTimeOffset`** ×3 (TASK-001/002 + رسالة unblock) — قرار #4. ⚠️ `ApiResponse.Timestamp` النهاردة `DateTime` (شغل من غير ticket).
- 🔴 **بيخلط بين «أملك» و«مستعير» في `IDisposable`** (TASK-006) → [TASK-006](tasks/TASK-006.md)
- 🟡 **بيحل مشكلة الـ compiler بدل مشكلة التصميم** (TASK-003 · رجع في TASK-013 r2 · أخف في TASK-014) → [TASK-004](tasks/TASK-004.md) · [TASK-013](tasks/TASK-013.md)
- 🟡 **بيعتمد على الـ default بتاع API من غير ما يفتح الـ signature** (TASK-004) → [TASK-004](tasks/TASK-004.md)
- 🟡 **بيحط guard في المكان الغلط** (TASK-005) → [TASK-005](tasks/TASK-005.md)
- 🟡 **comment بدل الحذف** ×3 — TASK-005 ×2، والتالتة في TASK-018 r1 (AuthController + `AuthService.cs:57`). اللي في TASK-007 اتسحبت لأنها كانت مقصودة. → لو طلب تاسك تبقى مرشحة. ⚠️ فيه `return Problem(...)` متعلّق عليه في [TranslationsController.cs:65-69](../TranslyAI.Api/Controllers/TranslationsController.cs#L65-L69) (شغل من غير ticket). → [TASK-005](tasks/TASK-005.md) · [TASK-007](tasks/TASK-007.md)
- 🔵 **تايبوهات في أسماء عامة** ×8 — آخرها `X-RateLimitLimit` (TASK-016، أول واحد على عقد خارجي). الحل الميكانيكي لو اتكرر: analyzer. → [TASK-015](tasks/TASK-015.md) · [TASK-016](tasks/TASK-016.md)
- 🔵 **مفيش newline في آخر الملفات** ×7.
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
- 🔺 **بيعترض بحجة لما يكون معاه حق** (`ApiKey` في `appsettings.json` — TASK-003)، و**بيقرا الـ ticket بعين ناقدة** ويعترض على الـ sequencing (TASK-005). → [TASK-003](tasks/TASK-003.md) · [TASK-005](tasks/TASK-005.md)
- 🔺 **بيتحقق من الافتراضات اللي الـ ticket مبني عليها** — اكتشف الـ Interactions API من التوثيق (TASK-007). → [TASK-007](tasks/TASK-007.md)
- 🔺 **بيتحقق بالبيانات الحقيقية** — `SELECT` واحد قفل قرار #18 (TASK-010). → [TASK-010](tasks/TASK-010.md)
- 🔺🔺 **صحّح الـ mentor مرتين:** بالتشغيل (`SqlState` — TASK-012)، وبمراجعة معمارية (المايجريشن تتولّد من الـ model مش تتكتب بالإيد — TASK-016). → [TASK-012](tasks/TASK-012.md) · [TASK-016](tasks/TASK-016.md)
- 🔺 **بيسأل على الدفاع الميت قبل ما يشحنه** (`[EmailAddress]` على الـ entity — TASK-012).
- 🔺 **عدّى فخاخ مقصودة من غير تلميح:** 503 مش 429 (TASK-005) · `RateLimited` و`DateTime.Today` (TASK-016).
- ضاف `OnAuthenticationFailed` من نفسه (TASK-013) · انضباط في rounds التصليح: المطلوب بس، صفر زيادة (TASK-015 r2) · بيسأل أسئلة مبدأ مش syntax — «تكرار النص في الداتابيز سليم؟» (TASK-017).

## ديون مفتوحة

> اتأكدت منها من الكود في 2026-09-23.

- **index زيادة** `HasIndex(usage => usage.UserId)` — [TranslyDbContext.cs:48](../TranslyAI.Api/Data/TranslyDbContext.cs#L48) — بيتصان مع كل `INSERT` ومفيش استعلام محتاجه. الخطوة 2 من مساره هو. → [TASK-016](tasks/TASK-016.md)
- **تسريب الـ charset** — `ReadAsStringAsync` في [GeminiApiService.cs:86](../TranslyAI.Api/Services/GeminiApiService.cs#L86) و`ReadFromJsonAsync` في [:104](../TranslyAI.Api/Services/GeminiApiService.cs#L104) بيرموا `InvalidOperationException` → 500 بدل 502. الحل المتحقق منه في [TASK-009](tasks/TASK-009.md). موعده M4.
- **guard ميت على `response.Model`** — [GeminiApiService.cs:145](../TranslyAI.Api/Services/GeminiApiService.cs#L145) — ترجمة ناجحة ممكن تترفض بـ 502 لو الحقل رجع فاضي. → [TASK-011](tasks/TASK-011.md)
- **مفتاح `["TargetLanguage"]` PascalCase + `"differnt"`** — [TranslationRequestDto.cs:26-27](../TranslyAI.Api/Dtos/TranslationRequestDto.cs#L26-L27) — نفس الـ endpoint بيرجّع مفاتيح camelCase وPascalCase. → [TASK-006](tasks/TASK-006.md)
- **`store: false` وبناء الـ prompt مالهمش حارس** — [GeminiApiService.cs:58](../TranslyAI.Api/Services/GeminiApiService.cs#L58) — قرار خصوصية محدش هيلاحظ لو اتشال. → [TASK-009](tasks/TASK-009.md)
- **ترتيب `RecordUsageAsync` قبل الكاش مالوش تيست** — دين على الـ mentor، مفيش مسار HTTP deterministic بيمسكه. → [TASK-015](tasks/TASK-015.md)
- **ديون M4:** `IExceptionHandler` مركزي → في TASK-018 · Serilog — لسه. ومفتاح `TargetLanguage` PascalCase اللي فوق بيتقفل بتيست الـ 400 في TASK-018.
- **`FallbackPolicy`** — اتعرضت 3 مرات ومتاخدتش.
- **migration فاضية** `20260831142943_EditTranslationUsage` لسه في الريبو — قراره. → [TASK-014](tasks/TASK-014.md)
- 🔵 `usedCountConsumed` في [TranslationService.cs:49](../TranslyAI.Api/Services/TranslationService.cs#L49) · `[AttributeUsage]` ناقصة على `SupportedLanguageAttribute`.
- **أسئلة مستحقة من غير إجابة** — متسجّلة في ملفات TASK-010 · 013 · 014 · 015 · 016.

## أسئلة مفتوحة مؤجلة لـ M8

- **الـ host** و**الـ Gemini tier** (Tier 1 = ربط billing account، سقفها $250) — لازم يتردّ عليهم قبل تقسيم M8.
- **HTTPS:** `UseHttpsRedirection` اتشالت خالص (قرار صح — الـ 307 على POST مش مناسب لـ mobile clients، والـ TLS بيتفك عند الـ edge). البديل: `UseHsts` + إجبار https عند الـ reverse proxy.
- **الدومين:** لسه متحددش. `api.transly.ai` في كود الفلاتر **placeholder** مش قرار.
- **الـ path prefix:** `/api` ممكن تتضاف عند الـ proxy (`UsePathBase`) من غير كود لو الدومين طلع من غير subdomain.
- **CI:** التيستات معتمدة على MySQL محلية و user secrets — لازم configuration بينقل (service container أو Testcontainers) قبل GitHub Actions.

## الجلسة الجاية

TASK-018 مفتوحة — لما يقول «خلصت» يبقى review بالكسرات المكتوبة في الـ ticket والفخاخ اللي في آخر ملفها. ولو طلب review للشغل اللي من غير ticket: #15 (interfaces) لسه مفتوح، و`IsEmailExistsAsync` بيعمل `ToLower()` على العمود ومن غير `CancellationToken`.
