# .NET Mentor — Progress

> ملف الحالة بتاع الـ mentor. بيتقرا في أول كل جلسة وبيتحدث في آخرها.
> مكانه: `mentor/progress.md` في الريبو بتاع الباك اند.

**آخر تحديث:** 2026-08-19

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

- **Milestone:** M2 — First real endpoints
- **التاسك المفتوحة:** مفيش — TASK-003 اتقفلت ✅ وهو طلب صريح إني مفتحش تاسك جديدة. يستنى «اديني تاسك».
- **⚠️ متعملش commit لسه:** ٥ ملفات جديدة + ٤ معدّلة لسه uncommitted وقت الـ review.
- **الوقت المتاح أسبوعياً:** ⏳ في انتظار الرد

### الشغال دلوقتي

- `GET /health` → `status` / `time` / `environment`
- `POST /v1/translations` → **ترجمة حقيقية من Gemini**. `GeminiApiService` (typed HttpClient) + `GeminiOptions` من user secrets. فشل المزوّد → 502 + الـ status والـ body الحقيقي في الـ log. body ناقص → 400 ProblemDetails مجاناً.

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
- 🔴 **`DateTime` بدل `DateTimeOffset` — 3 مرات** (TASK-001 r1، TASK-002 r1، وبعد ما اتنبّه في رسالة الـ unblock). القرار #4.
- 🟡 **بيحل مشكلة الـ compiler بدل مشكلة التصميم — جديدة في TASK-003.** طلعله `CS8604` فخلّى الـ record `string?` عشان الـ warning يسكت، بدل ما يضمن القيمة. الـ warning بيختفي والمشكلة بتنتقل للي بعده. **يتراقب** — لو اتكرر يبقى تاسك عن nullable reference types.
- 🟡 **بيتعامل مع الـ guard كأنه العقبة مش الأداة.** عمل comment على `EnsureSuccessStatusCode()` عشان يوقف الـ 500، فخلّى الفشل يعدّي صامت. الغريزة "أشيل اللي بيزعق" بدل "أتصرف في اللي بيزعق منه".
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
| — | **الـ AI proxy** | ✅ | اتعمل في TASK-003: `AddHttpClient<GeminiApiService>` + `GeminiOptions` من user secrets + prompt بالنبرات التلاتة + 502 على فشل المزوّد. **الباقي منه:** `finishReason`، `ValidateOnStart` للـ key، Timeout، تثبيت اسم الموديل |
| M3 | EF Core | ⬜ | حفظ الترجمات + **نقل الـ history** من `history_local_datasource` للسيرفر (`HistoryEntry` فيها `isFavorite`) |
| M4 | Validation / errors / logging | ⬜ | شكل الخطأ يطابق `sealed class Failure` عند الـ client (Network/Server/Cache/Offline/Unknown) |
| M5 | Auth (JWT) | ⬜ | مفيش auth في التطبيق دلوقتي — تصميم من الصفر على الجهتين |
| — | **العدّاد والاشتراكات** | ⬜ | **السبب اللي اتبنى عشانه الباك اند.** عدّ الاستهلاك (حروف ولا requests؟ — راجع درس `char`/`Rune`)، خطط، quota في `X-RateLimit-*` headers، رفض 429 |
| — | **Streaming (SSE)** | ⬜ | الترجمة تظهر تدريجياً بدل انتظار الرد كامل — مكسب حقيقي في UX لتطبيق ترجمة |
| M6 | Production concerns | ⬜ | caching للترجمات المتكررة (نفس النص + نفس الزوج = نفس الناتج — توفير مباشر في فاتورة الـ AI) |
| M7 | Testing | ⬜ | — |
| M8 | Docker + CI + نشر | ⬜ | الدومين، HTTPS عند الـ edge، وتوجيه `ApiEndpoints.baseUrl` على المنشور |

## الجلسة الجاية

مفيش تاسك مفتوحة — طلب صريح إني مفتحش واحدة جديدة. يستنى منه «اديني تاسك».

**أول حاجة تتقال له:** يعمل commit — الشغل كله لسه uncommitted.

**المرشحين لـ TASK-004، بالترتيب:**

1. **`TranslationTone` enum** بدل الـ string (~30-45د) — دلوقتي `"tone":"banana"` بيرجع 200 وبيروح على `default` من غير أي تعليمة نبرة. بقت أهم بكتير بعد TASK-003 لأن النبرة بقت بتدخل في الـ prompt فعلاً. بتعلّم `JsonStringEnumConverter` والـ `switch` expression مع بعض. **دي الأنسب.**
2. **تقوية الـ AI proxy** (~60د) — الـ 🟡 التلاتة من review TASK-003: `finishReason` في الـ log، `ValidateOnStart()` للـ key، وفصل `ModelVersion` الفاضية عن الفشل. تاسك "خلّي اللي بنيته صالح للإنتاج".
3. **`GET /v1/languages`** (~45د) — أول collection response وأول قرار envelope في الـ lists.
4. **M3 — EF Core** — حفظ الترجمات ونقل الـ history من الـ device.

## ملاحظات من TASK-003 تتبني عليها بعدين

- **الـ free tier بتاع Gemini = 20 request/دقيقة** على `gemini-3.7-flash`. ده الرقم اللي هيتبني حواليه العدّاد والـ quota.
- `gemini-flash-latest` alias بيتحل لـ `gemini-3.7-flash` النهاردة. **يتثبت قبل الإنتاج.**
- Gemini بيرجّع `finishReason` (`SAFETY` / `MAX_TOKENS` / `RECITATION`) وإحنا مش عاملينه deserialize — ده اللي هيفسّر الترجمات الفاضية.
- `[ApiController]` + `required` بيدوا `ProblemDetails` كاملة بأسماء الحقول الناقصة مجاناً — اتأكد عملياً. نبني عليها في M4 بدل ما نبدأ من الصفر.

**درس اتزرع ولسه هيتحصد:** `char` في C# = UTF-16 code unit مش حرف. اتبيّن عملياً في stub الترجمة (الإيموجي بقى `��` بعد `Array.Reverse`). نرجعله لما نحسب استهلاك الحروف للـ quota — `StringInfo` / `Rune`.
