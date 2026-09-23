# قرارات معمارية وقواعد العمل — Transly

> اتنقل من `progress.md` في 2026-09-23 (نص القرارات زي ما هو). القرار الجديد بيتضاف صف في الجدول، والقرار اللي بيتقلب بيتعلّم جنبه ومايتمسحش.

## ⚠️ قرارات الكود الحالي مختلف عنها

> اتغيّرت في commits من غير ticket (`4ccca52` · `ff7fdd2` · `07b472f`، 2026-09-22/23) ولسه متراجعتش. #7 و#11 اتحسموا بقراره 2026-09-23 (اتقلبوا — شوف الجدول تحت).

| # | القرار | الكود النهاردة | الحالة |
|---|---|---|---|
| 4 | `DateTimeOffset` مش `DateTime` | `ApiResponse.Timestamp` نوعه `DateTime` | بيتصلح في TASK-018 (ب) |
| 7 | `/v1/...` والـ `/api` تتضاف عند الـ proxy | الـ controllers على `api/v1/[controller]` | ✅ اتقلب بقراره 2026-09-23 |
| 11 | مفيش envelope | `ApiResponse<TData>` فيه `Success`/`StatusCode`/`Message`/`Data`/`Errors`/`Timestamp` | ✅ اتقلب بقراره 2026-09-23 |
| 15 | مفيش interface بـ implementation واحدة | `IAuthService` · `IGeminiApiService` · `ITranslationService` | ⬜ مفتوح |

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

## قرارات معمارية اتاخدت

| # | القرار | اخترنا | البديل المرفوض | السبب باختصار |
|---|---|---|---|---|
| 1 | شكل الـ solution | مشروع واحد (`TranslyAI.Api`) | 4 projects: Api/Application/Domain/Infrastructure | الـ API لسه endpoint أو اتنين — الـ layering دلوقتي ceremony مش testability. نقسّم لما يبقى فيه سبب حقيقي |
| 2 | API style | Controllers | Minimal APIs لكل حاجة | الـ API رايح على auth + subscriptions. بس `/health` فضل `MapGet` لأنه infrastructure مش domain |
| 3 | مكان الـ Gemini key | على السيرفر بس | obfuscation أو تشفير الـ key في الـ APK | أي key بيوصل للـ device بيتسرب — decompile أو HTTPS proxy. مفيش حل client-side |
| 4 | نوع الوقت | `DateTimeOffset` | `DateTime` | `DateTime.Kind` بيضيع على أول boundary (DB/serialization). الـ offset جوه القيمة في `DateTimeOffset` |
| 5 | شكل حقل `status` | string (`Healthy`/`Degraded`/`Unhealthy`) | `bool ok` | الـ bool مبيعرفش يعبر عن Degraded، وتغيير الـ contract بعدين مكلف على mobile clients |
| 6 | نوع الـ DTOs | `record` | `class` | immutability + value equality + `with` من غير codegen — الـ Freezed بتاعته بس جوه اللغة. الـ `class` للـ entities اللي ليها identity |
| 7 | الـ route والـ status | ↩️ **الـ prefix اتقلب 2026-09-23 بقراره: `api/v1/...`** — مفيش client في السوق فالتغيير مكلّفش حاجة. التمن المعروف: لو الدومين طلع `api.*` الـ path هيبقى فيه api مرتين. الـ resource والـ 200 زي ما هم. **الأصل:** `POST /v1/translations` بـ 200 | `/translate` (فعل)، `/api/v1/...`، أو 201 + Location | **الـ resource:** feature الـ history هتتنقل للسيرفر → `GET /v1/translations` نفس الـ resource، والفعل مبيديش الشكل ده. **الـ prefix:** قرار رخيص وقابل للرجوع — `/api` ممكن تتضاف في الـ reverse proxy وقت النشر (`UsePathBase`) من غير كود. الـ `/v1` هو اللي مش رخيص، لأنه اللي بيسمح بكسر الـ contract من غير كسر التليفونات في السوق. **الـ status:** 201 من غير resource محفوظة تبقى كذبة — الـ Location هيرجع 404 |
| 8 | شكل الـ DTO | مسطّح — `sourceLanguage: "en"` | نسخ `LanguagePair` المتداخلة بكل حقول `Language` | السيرفر محتاج الكود بس؛ `name`/`isRtl` حاجات عرض. شكل الـ wire ≠ شكل الـ entity |
| 9 | المحرّك في الـ response | `model: "stub"` → بعدين اسم الموديل | نسخ `enum { mlKit, ai }` | السيرفر عمره ما هيشغّل ML Kit. والـ string بيقول **أي** موديل ترجم — يفرق في debugging وتغيير الموديلات. الـ client بيعمل map لـ `TranslationEngine.ai` في الـ data layer |
| 10 | `id` في الـ response | **مفيش دلوقتي** | `id` مولّد، أو `int` زي `HistoryEntry` | مفيش persistence فالـ id كذبة. إضافة حقل بعدين مش breaking. ولما ييجي يبقى `string` — الـ int التسلسلي بيسرّب حجم الاستخدام وبيسمح بالعد |
| 11 | الـ envelope | ↩️↩️ **2026-09-23 بقراره: `ApiResponse<T>` للنجاح (2xx) وProblemDetails للفشل (4xx/5xx)** — في نفس اليوم اختار الأول envelope على كل رد، وبعد r1 من TASK-018 استقر على الشكل المختلط («انا عايز شكل الريسبونس كدا»). الترشيح كان ProblemDetails للفشل والـ resource للنجاح. الشرط: الـ envelope مايقدرش يوصف فشل (بيتشال منه `Success`/`StatusCode`/`Errors`)، والتيستات بتفرض القاعدة على كل رد. **الأصل:** مفيش — الـ object مباشرة | `{ "data": {...} }` | HTTP فيه status codes و`ProblemDetails`. الـ envelope بس لما يكون فيه metadata جنب البيانات (pagination) |
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
| 25 | شكل الـ identity | جدول `User` بإيدنا + `IPasswordHasher<User>` من package الـ Identity **لوحدها** | ASP.NET Core Identity الكامل (`IdentityDbContext` + `UserManager`) · `BCrypt.Net-Next` · hashing بإيدنا | Identity الكامل = **7 جداول** و~15 عمود على `IdentityUser` مش هيتلمسوا (`TwoFactorEnabled`, `LockoutEnd`, `SecurityStamp`…) — نفس منطق قرار #1 و#15. **بس الـ hashing نفسه ماينكتبش بإيد:** `PasswordHasher<T>` = PBKDF2-HMAC-SHA512 بـ 100k iteration + salt لكل مستخدم + مقارنة fixed-time + **رقم نسخة جوه الـ string** يسمح بترقية الخوارزمية من غير كسر المستخدمين. `SHA256(pwd+salt)` سريعة **بالتصميم** = GPU بيجرّب بلايين في الثانية. **BCrypt خيار محترم** — اترفض بس لأنه package زيادة تعمل نفس اللي `Microsoft.AspNetCore.Identity` هتعمله وهي نازلة أصلاً مع الـ JWT. **يترجع للقرار** أول ما نحتاج اتنين من: email confirmation · lockout · 2FA · external logins · roles |
| 26 | شكل الهوية في Transly | ✅ **حساب حقيقي (email + password)** — اختياره 2026-08-30 | هوية على الجهاز بس (anonymous quota) · anonymous يترقّى لحساب عند الاشتراك | قراره. الـ email+password هو الـ slice الأنضف للتعلّم (بيغطي hashing + JWT + claims + `[Authorize]` + refresh) وهو الأساس اللي أي شكل تاني بيتبني فوقه. الـ anonymous ممكن يتضاف بعدين من غير كسر. **والتطبيق مفيهوش أي auth حالياً** (سؤال intake #3 اتقفل) — يعني تصميم من الصفر على الجهتين ومفيش مزوّد خارجي نتفاوض معاه |
| 27 | الفشل المتوقع من service مالوش غير مسار واحد | ❌ **اتسحب 2026-09-23 قبل التنفيذ** — مع ProblemDetails الـ framework بيطلّع الـ 500 لوحده، ومفيش exception معروفة تانية محتاجة `IExceptionHandler`، فـ `null` (قرار #16) كفاية لمسار الـ 409. يرجع أول ما يبقى فيه exception معروفة محتاجة map. **النص الأصلي:** exception مخصوصة (`EmailAlreadyTakenException`) بتتحول لـ status في `IExceptionHandler` واحد — TASK-018، 2026-09-23 | `InvalidOperationException` + map · `null` · result type | الـ caller مالوش حاجة يعملها غير إنه يعدّيه، وحالة الـ race أصلاً exception من EF Core. **`InvalidOperationException` مرفوضة** لأن الـ BCL بيرميها في bugs — لو ليها map لـ 409 الـ bug هيوصل للعميل على إنه «الإيميل موجود». **الـ result type بيفضل الصح** لما الـ caller بيقرر حاجة مختلفة لكل حالة (`TranslationOutcome`: 8 حالات + headers). الاتنين عايشين في الكود بقصد. تطبيق لقرار #16 |
| 28 | بناء الـ DTOs من الـ entities | ✅ **AutoMapper — قراره 2026-09-23** · `Profile` لكل entity في `TranslyAI.Api/Mapping/` · `ProjectTo` في الاستعلامات · تيست `AssertConfigurationIsValid` على كل الـ profiles · من غير `ReverseMap` | `UserDto.From(user)` بإيد (كان موجود) · الاتنين مع بعض (اللي كان حاصل) | قراره. **الشرط اللي اتحط:** طريقة واحدة بس لبناء الـ DTO — الـ bug (`CreatedAt = 0001-01-01` في الـ login) جه من طريقتين بيبنوا نفس الـ DTO. **والـ guard لازم يبقى تيست configuration**، مش مقارنة login بـ `/me`: لما الاتنين بقوا ماشيين على نفس الـ mapping، الـ break check أثبت إن تيست المقارنة بيفضل أخضر على الـ bug. `ReverseMap` اتشال: مفيش حد بيعمل map من DTO لـ entity، ولو حصل يبقى overposting. **التمن:** الـ license (AutoMapper من 15 تجاري — Lucky Penny)، والأخطاء بتبان في الـ runtime مش الـ compile |
| 22 | `required` على DTO جاي من مزوّد خارجي | ✅ **اتطبق في r2** — كل الحقول nullable، والـ enum بيتبني من `string?` بـ `MapFinishReason` مع `Unknown` | `required` على كل حقل · `JsonStringEnumConverter` مباشرة على الـ enum | `required` + System.Text.Json = `JsonException` لما الحقل ينقص. ده صح للـ **مدخلات بتاعتنا** (الـ binder بيحوّلها 400 ProblemDetails)، وغلط للـ **ردود المزوّد** — الحقل الناقص هناك مش خطأ عميل، دي حالة معروفة (`promptFeedback.blockReason`) والـ exception بتخبّيها |

## قواعد العمل — اتفرضت منه أثناء البرنامج

> كلها بقت جزء من الـ skill نفسها (`.claude/skills/dotnet-mentor/SKILL.md` — قسم Standing rules). النصوص تحت هي الأصل بتاريخه وسببه.

## 🔴 قاعدتين جديدتين اتفرضوا منه (2026-08-24) — تسري على كل الجلسات الجاية

1. **الـ API client-agnostic تماماً.** متقراش ولا تتأثر بكود تطبيق الفلاتر ولا أي front end. الشكل اللي الكلاينت عامل بيه الحاجة **مش معيار**. الوحيد اللي يتحسب: إن الـ endpoints مستهدفة موبايل (payload، latency، pagination، error contract، versioning). لو ناقصني requirement — **أسأله بالكلام**، مش أقرا الفلاتر.
   - **الأثر الفوري:** قسم "الـ Contract المستخرج من تطبيق الفلاتر" تحت بقى **أرشيف تاريخي**، مش مرجع. `Language.wellKnown` و `HistoryEntry` وغيرهم مايتنسخوش — أي contract جاي يتصمم من الصفر.
2. **كلمة "ساعدني" = الكود كامل + الشرح في الدردشة.** مش hint ladder، مش أسئلة توجيهية. الـ hint ladder بتاعة الـ skill تفضل شغالة على "متعلق"/"مش فاهم"، بس **"ساعدني" بتقفز على طول للمستوى الكامل** — الكود اللي المفروض يكتبه أو يعدّله + ليه. في الدردشة (code block)، مش تعديل مباشر على ملفاته.
   - **إضافة اتفق عليها:** بعد الكود، أسأله سؤال أو اتنين عليه. وكلمة **"لمحة"** اختيارية لو عايز دفعة بس.

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

### مفيش سؤال عن الوقت (2026-09-02)

4. ✅ ~~الوقت الأسبوعي بالساعات~~ — **اتقفل بقرار منه 2026-09-02، والسؤال ممنوع يتسأل تاني.** نصه: **«مالكش دعوة انا وقتي المتاح كم، اعطني التاسك وشوف الحجم المناسب وانا اشتغل عليها سواء خلصتها في نفس اليوم او بكرة او بعد اسبوع ما مهم، المهم اخلصها وافهمها.»**
   - **الأثر على كتابة أي ticket جاي:** التاسك **تتحجّم بالمحتوى** (كام جزء، وكل جزء بيعلّم إيه) مش بالوقت المتاح. **وأبواب الخروج المعلنة** (اللي اتحطت في TASK-015 و016 عشان ما أخمّنش وقته) **مبقاش ليها سبب** — بدلها: كل جزء يتقفل على نفسه بحيث ينفع يسيبها ويرجعلها.
   - **وده بيسحب أهم عذر كنت بعلّقه على الحجم.** التاسك تبقى بحجمها الصح، والسرعة مش معياري.

## أرشيف — خريطة الـ milestones زي ما كانت في progress.md قبل الضغط

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
| M4 | Validation / errors / logging | ⏭️ **اتخطّت بوعي (2026-08-30)** | الدين عليها كله في مسار الترجمة (تسريب الـ `charset` في `GeminiApiService` + مفتاح `TargetLanguage` PascalCase) **ومش واقف في طريق الـ auth**. الـ `[ApiController]` بيدي `ProblemDetails` مقبولة على endpoints الـ auth مجاناً. تترجعلها بعد ما الـ auth تقف. شكل الخطأ يطابق `sealed class Failure` عند الـ client |
| M5 | Auth (JWT) | 🚧 **هنا** | ✅ اتحسم: **email + password، مفيش auth في التطبيق حالياً** (قرار #26)، وجدول بإيدنا + `IPasswordHasher` مش Identity الكامل (قرار #25). **TASK-012 ✅:** `User` + register + hashing. **TASK-013 ✅:** login + إصدار JWT + `[Authorize]` + `/me`. **TASK-014 ✅:** قفل `/v1/translations` + جدول `TranslationUsages` per-user + **أول علاقة FK في الداتابيز** (cascade + index، متحقق منهم بالتشغيل). **الترتيب اتقلب بقصد** — refresh tokens اتأخرت بعد TASK-014، لأن الـ refresh مالوش وزن قبل ما يكون فيه حاجة تستاهل تفضل logged in عشانها. الجاي بعدها (خريطة مش تاسكات): `GET /v1/translations` (history + pagination) → refresh tokens → quota لكل حساب |
| — | **العدّاد والاشتراكات** | ⬜ | **السبب اللي اتبنى عشانه الباك اند.** عدّ الاستهلاك (حروف ولا requests؟ — راجع درس `char`/`Rune`)، خطط، quota في `X-RateLimit-*` headers، رفض 429 |
| — | **Streaming (SSE)** | ⬜ | الترجمة تظهر تدريجياً بدل انتظار الرد كامل — مكسب حقيقي في UX لتطبيق ترجمة |
| M6 | Production concerns | ⬜ | caching للترجمات المتكررة (نفس النص + نفس الزوج = نفس الناتج — توفير مباشر في فاتورة الـ AI) |
| M7 | Testing | 🚧 **راجعة 2026-08-31 بسبب حقيقي — TASK-015** | TASK-008 ✅: الـ seam هو `HttpMessageHandler` مش interface (قرار #15 قايم). TASK-009 ❌ اتلغت. **TASK-015 🚧 (اتسلّمت 2026-08-31):** `WebApplicationFactory` + داتابيز MySQL حقيقية + الـ Gemini stub. **السبب مش coverage** — الفوترة في `TranslationService` مالهاش حارس، وطقس الـ curl اليدوي اتطلب 7 مرات. الرجوع لـ M7 المرة دي **مش بالعطالة** (درس TASK-009): السبب اتولد من review TASK-014 نفسه |
| M8 | Docker + CI + نشر | ⬜ | الدومين، HTTPS عند الـ edge، توجيه `ApiEndpoints.baseUrl` على المنشور، و**تثبيت اسم الموديل** بدل `gemini-flash-latest` (اتنقلت من TASK-005 — قرار #18) |
