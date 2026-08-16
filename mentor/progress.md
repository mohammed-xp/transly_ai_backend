# .NET Mentor — Progress

> ملف الحالة بتاع الـ mentor. بيتقرا في أول كل جلسة وبيتحدث في آخرها.
> مكانه: `mentor/progress.md` في الريبو بتاع الباك اند.

**آخر تحديث:** 2026-08-15

---

## المشروع

- **التطبيق:** Transly AI — تطبيق Flutter لترجمة النصوص بالـ AI
- **الـ slice المتفق عليه:** الترجمة end-to-end من خلال الباك اند — التطبيق يبعت النص واللغة للـ API، والـ API هو اللي بيكلم Gemini ويرجع الترجمة. الـ Gemini key يقعد على السيرفر بس.
- **باك اند موجود قبل كده؟** لأ.
- **تصحيح مهم (2026-08-15):** التطبيق **مش** بينادي Gemini ولا أي AI. الـ `AiTranslationStubDatasource` بيرجّع `'[AI stub] $text'` بعد delay. الترجمة الحقيقية الشغالة = **ML Kit on-device**، والـ `TranslationRepositoryImpl` بيختار بينهم بالـ connectivity. يعني **مفيش key في التطبيق يتسرب** — الباك اند بيملا فراغ، مش بيصلح خطر قايم. الدافع الحقيقي = الاشتراكات + الـ quota.
- **⚠️ سؤال مفتوح:** كوده بيقول **Claude** (`api_endpoints.dart` + التعليق في الـ stub)، وهو قال **Gemini**. مش محسوم لسه. معماريًا مش بيغير الـ endpoint.
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
- **التاسك المفتوحة:** TASK-002 — عقد الترجمة (fake logic، من غير Gemini)
- **الوقت المتاح أسبوعياً:** ⏳ في انتظار الرد

## التاسكات

| ID | العنوان | Milestone | الحالة | نتيجة الـ review |
|---|---|---|---|---|
| TASK-001 | تنضيف الـ template + `/health` + أول commit | M0 | ✅ Done | Approved في الـ round التالت — اتنين rounds اتضاعوا في acceptance criteria متقروش |
| TASK-002 | `POST /api/translations` بـ records و fake logic | M2 | 🚧 In progress | — |

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

## مفكرة الـ mentor — نقاط بتتكرر

> لما حاجة تتكرر 3 مرات، تتحول لتاسك مخصصة ليها.

- ⚠️ **بيبعت الشغل للمراجعة من غير ما يعدّ الـ acceptance criteria — اتكررت مرتين (TASK-001 rounds 1 و 2).** نفس الملاحظة (`status` الناقص) رجعت مرتين ورا بعض. اتقالت له صريحة في الـ round التاني. **المرة التالتة → نوقف الـ curriculum ونعمل تاسك مخصصة.**
- بيتّبع الـ snippets الجاهزة حرفياً من غير ما يسأل هي بتعمل ايه (GitHub's "create a new repository on the command line" → commit فيه README بس)
- ميل واضح لنقل Clean Architecture من الـ Flutter كما هي (عمل 3 فولدرات layers قبل ما يكتب endpoint واحد) — استجاب للـ pushback من غير جدال

## نقاط قوة

- Senior Flutter — DI، layering، async، REST، JSON، auth flows، CI/CD كلها مفاهيم مفهومة. السينتاكس بس الجديد.
- بيسمع للـ pushback لما يكون معاه سبب، مش نبرة. مسح الـ 3 فولدرات من غير جدال.
- بيسأل أسئلة صح قبل ما ينفذ ("الـ status يكون فيها ايه؟") بدل ما يخمن.
- Conventional Commits من نفسه من غير ما حد يطلبها.
- عنده تمارين .NET قديمة في `source/repos` (HR.LeaveManagement, BookStoreApp, MyFirstApi) — لسه محتاجين نعرف وصل فيها لفين

## أسئلة intake لسه مجاوبش عليها

1. ✅ ~~فين ريبو الفلاتر؟~~ — `mohammed-xp/transly_ai`، اتقرا والـ contract اتستخرج
2. مستوى الـ C#: LINQ و EF Core migrations عملهم بإيده قبل كده ولا لأ؟
3. التطبيق فيه users/Firebase Auth دلوقتي؟ (بيحدد شكل M5) — مفيش feature auth في الريبو، فالأغلب لأ
4. الوقت الأسبوعي بالساعات
5. Gemini ولا Claude؟ (كوده بيقول Claude، هو قال Gemini)

## الجلسة الجاية

review لـ TASK-002 — أول controller وأول records. وقبل الـ review، قراية كود الفلاتر لو بعته وتعديل الـ contract على أساسه.
