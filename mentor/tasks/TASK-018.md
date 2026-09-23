# 🎫 TASK-018 — error contract واحد: `ApiResponse<T>` للنجاح وProblemDetails للفشل

**Milestone:** M4 — Validation / errors / logging · **الأجزاء:** 4 · **الصعوبة:** ▓▓▓░░

> **اختياره** من 3 اتجاهات اتعرضت 2026-09-23 (CI · M4 · refresh tokens)، والترشيح المعلن كان CI.
> **قرارين خدهم قبل ما التاسك تتكتب:** envelope على كل رد (قرار #11 اتقلب، والترشيح كان ProblemDetails) · تثبيت `api/v1` (قرار #7 اتقلب).
> وقت الكتابة كان بدأ الجزء (أ) في الـ working tree: الـ test project رجع في الـ `slnx` والـ compile اتصلح. الـ routes وقراية الـ envelope لسه.
>
> **↩️ اتعدّلت بعد r1 (2026-09-23):** في r1 كان الفشل بقى ProblemDetails والنجاح envelope. اتسأل يحسم، ورد: **«انا عايز شكل الريسبونس كدا»** — يعني الشكل المختلط ده هو القرار. الـ ticket اتعاد كتابته عليه: الـ envelope بقى للنجاح بس، و`IExceptionHandler` و`EmailAlreadyTakenException` اتشالوا (الـ framework بيطلّع الـ 500 لوحده، والـ `null` في `RegisterAsync` كفاية لمسار واحد — قرار #16 قايم و#27 اتسحب). النص الأصلي قبل التعديل في git history.

---

## الهدف

الـ API ليه قاعدة واحدة العميل يقدر يعتمد عليها: **2xx = `ApiResponse<T>`، و4xx/5xx = ProblemDetails (`application/problem+json`)**. من غير استثناء: الـ validation، والـ 401 من الـ middleware، والـ route اللي مش موجود، والـ exception اللي محدش مسكها. ومفيش رد بيسرّب تفاصيل من جوه السيرفر.

## ليه دلوقتي

القاعدة دي بتشتغل بس لو مفيش ولا رد بيكسرها. النهاردة `/me` لسه بيرجّع أخطاء بالـ envelope ومعاها `ex.Message`، والـ envelope نفسه لسه قادر يوصف فشل (`Success = false`، `Errors`، `ApiResponse.Conflict(...)`) مع إن مفيش حد المفروض يستخدمه كده. وكمان حجة تأجيل M4 خلصت من بدري.

---

## المطلوب

### أ — التيستات ترجع تحرس

- [ ] `TranslyAI.Api.Tests` راجع في `TranslyAI.slnx`، و`dotnet test` من جذر الريبو بيشغّل كل التيستات.
- [ ] التيستات بتضرب الـ routes الحالية `api/v1/...`، وبتقرا الردود من خلال helper واحد في `TranslyAI.Api.Tests/Integration/ApiResponseAssert.cs`.
- [ ] كل التيستات الموجودة خضرا، **ومفيش ولا request بيطلع لـ Gemini الحقيقي** من أي تيست.
- [ ] تيست جديد: ترجمة ناجحة بترجّع `data.translatedText` بنفس النص اللي الـ stub رجّعه.

### ب — القاعدة تبقى مستحيل تتكسر

- [ ] `ApiResponse<T>` مايقدرش يوصف فشل: يتشال منه `Success` و`StatusCode` و`Errors` وكل الـ factories اللي بتبني فشل (`BadRequest` · `Conflict` · `TooManyRequests` · `Error` · `NotFound`). اللي يفضل حاجة بتتقال على النجاح بس.
- [ ] `Timestamp` نوعه `DateTimeOffset` (قرار #4).
- [ ] `ApiResponseAssert` بيوقّع أي تيست لو رد 2xx مش envelope، أو رد ≥400 مش `application/problem+json`، أو `status` في الـ ProblemDetails مختلف عن الـ HTTP status.

### ج — الردود اللي الـ framework بيكتبها لوحده

التيستات في `TranslyAI.Api.Tests/Integration/ErrorContractTests.cs`:

- [ ] طلب ترجمة فيه لغة المصدر هي هي لغة الهدف → 400 ProblemDetails، و`errors` فيه المفتاح `targetLanguage` بالظبط زي ما العميل بيبعته في الـ JSON.
- [ ] طلب من غير token → 401 ProblemDetails، **و`WWW-Authenticate: Bearer` لسه موجود** (التيست الموجود بيحرسه).
- [ ] login بباسورد غلط → 401 ProblemDetails.
- [ ] route مش موجود (`GET /api/v2/translations`) → 404 ProblemDetails.

### د — مفيش رد بيسرّب

- [ ] مفيش `catch (Exception)` في أي controller (`/me` آخر واحد).
- [ ] exception محدش مسكها → 500 ProblemDetails، والـ body مفيهوش رسالة الـ exception ولا الـ stack trace. تيست بيثبت ده (الـ `GeminiStubHandler` يقدر يرمي exception).
- [ ] تسجيل نفس الإيميل مرتين → 409 ProblemDetails. تيست.

---

## خارج الـ scope

- **Serilog وcorrelation IDs** — دول M4 برضه بس تاسك لوحدهم.
- **`IExceptionHandler` مخصوص** — مفيش exception معروفة محتاجة map لـ status غير 500، والـ 500 الـ framework بيطلّعه لوحده بـ `AddProblemDetails` + `UseExceptionHandler`. أول exception معروفة تظهر، يتضاف.
- **حقل `code` في الـ ProblemDetails** (زي `email_taken`) — بيتضاف لما يبقى فيه status واحد بيعني حاجتين العميل لازم يفرّق بينهم. النهاردة مفيش.
- **403** — مفيش policy ولا role بيطلّعه، فأي معالجة ليه دفاع ميت.
- **`/health`** — اللي بيستهلكه الـ load balancer مش التطبيق. يفضل زي ما هو.
- **تسريب الـ charset** في `GeminiApiService` — بعد التاسك دي هيطلع 500 ProblemDetails سليم، بس الـ status الصح (502) تاسك تانية.
- الـ interfaces وAutoMapper — قرار #15 لسه مفتوح ومش هنا.
- Testcontainers وCI.

---

## مفاتيح تدور بيها

- `AddProblemDetails` · `UseExceptionHandler` · `UseStatusCodePages` — [Handle errors in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling)
- [Automatic HTTP 400 responses](https://learn.microsoft.com/aspnet/core/web-api/#automatic-http-400-responses) — الـ `ValidationProblemDetails` بييجي منين
- `ValidationResult` member names · `JsonPropertyName` — عشان مفتاح `targetLanguage`
- `Accept: application/json` مع الـ developer exception page — وليه الـ 500 في الـ Development شكله مختلف عن الـ Testing

---

## Definition of Done

`dotnet test` من جذر الريبو أخضر وفيه التيستات الجديدة، و`dotnet build --no-incremental` بصفر warnings، و`dotnet format --verify-no-changes` نضيف.

---

## 🧭 قرار #11 اتقلب بقرارك: `ApiResponse<T>` للنجاح، ProblemDetails للفشل

- **اخترنا:** 2xx = envelope، و4xx/5xx = ProblemDetails. العميل بيفرّق بالـ status code، مش بحقل جوه الـ body.
- **البدائل:** envelope على كل رد (اختيارك الأول) · ProblemDetails للفشل والـ resource مباشرة للنجاح (الترشيح الأصلي).
- **ليه الشكل ده شغال:** الفشل كله بقى ببلاش من الـ framework: الـ 400 والـ 404 والـ 401 من الـ middleware والـ 500. وده اللي كان هيتعمل بإيدك لو فضل envelope على كل رد.
- **التمن:** الـ envelope بيلف كل نجاح في `data` من غير metadata جنبه، والعميل بيفك مستوى زيادة في كل رد. ولو جت pagination، الـ metadata مكانها جوه الـ envelope، وساعتها يبقى ليه سبب حقيقي.
- **الغلطة الشائعة:** إن الـ envelope يفضل قادر يوصف فشل. بعد سنة حد هيكتب `ApiResponse.Error(...)` بـ status 200، والقاعدة تتكسر من غير ما حد ياخد باله. عشان كده الجزء ب بيشيل الإمكانية دي من النوع نفسه، مش بيعتمد على الانتباه.

---

## ⬜ اللي هكسره بنفسي في الـ review

| الكسر | التيست اللي لازم يحمرّ |
|---|---|
| أرجّع `return Ok()` من غير body | تيست الترجمة الناجحة |
| أشيل `UseStatusCodePages` | تيست الـ 401 أو الـ 404 |
| أرجّع `ex.Message` في رد الـ 500 | تيست الـ 500 |
| أخلّي `RegisterAsync` يرمي بدل ما يرجّع `null` في الـ duplicate | تيست الـ 409 |
| أرجّع رد فشل بـ `Ok(...)` في مسار واحد | أي تيست بيعدّي على المسار ده |

تيست يفضل أخضر بعد أي كسر من دول = finding.

---

## 🔍 r1 (2026-09-23، «شوف كدا» — شغل لسه ماشي) — 🔁 Changes requested

**اتشغّل:** `build --no-incremental` 0 warnings (الـ 7 `CS0162` اتشالوا) · `format` نضيف · `dotnet test` → **8 فشلوا / 6 عدّوا / 14** — كل الـ integration tests بـ `Expected: Unauthorized, Actual: NotFound` (لسه بتضرب `/v1/...`). الـ break check اتأجل: مفيش معنى ليه والتيستات حمرا لسبب تاني.

**🔴 Blockers:**
1. **شكلين للـ contract:** النجاح `ApiResponse<T>`، والفشل في register/login/translations بقى ProblemDetails (`ProblemExtensions` + `AddProblemDetails`). ده عكس اختياره (envelope على كل رد) وعكس الـ ticket. **اتسأل:** غيّر قراره لـ ProblemDetails ولا لأ؟ لو غيّر، النجاح يرجع الـ resource مباشرة وقرار #11 يرجع لأصله.
2. **الجزء (أ) مش متعمل:** الـ routes في التيستات + `return Ok();` لسه في `TranslationsController.cs:61`.
3. **`/me` لسه فيه `catch (Exception)` + `ex.Message` + `return Unauthorized()`** (`AuthController.cs:105-130`) — التسريب لسه عايش.

**🟡:** كود متعلّق عليه بدل الحذف (AuthController كتير + `AuthService.cs:57`) — **التالتة للنمط**. `Result<T>` و`Error` في `Common/` مش مستخدمين خالص — **اتسأل هو ناوي عليهم ايه**. `RegisterAsync` بيرجع `null` بدل `EmailAlreadyTakenException` — شغال وبيغطي الـ race، بس مش اللي الـ ticket طالبه، ولو عنده حجة يقولها.

**🟢:** ترتيب الـ middleware صح من أول مرة (`UseExceptionHandler` + `UseStatusCodePages` قبل الـ auth — الفخ المكتوب تحت اتعدّى) · الـ title بيتحسب من الـ status في مكان واحد (`ProblemWithStatus`) · الفحص المكرر في الـ controller اتشال والحالتين (الفحص والـ race) بيدّوا نفس الإشارة.

**↩️ رده على البلوكر 1:** «انا عايز شكل الريسبونس كدا» → الشكل المختلط هو القرار. الـ ticket اتعاد كتابته (شوف أول الملف). وشال `Result<T>` و`Error` بعد الـ review. البلوكر 2 و3 قايمين.

**⚠️ «بيغيّر جزء من الـ contract ويسيب الباقي» — ممكن تبقى الرابعة (`/me`)، بس الشغل لسه ماشي فممكن يكون مجاش لها. متسجّلتش لحد ما يتأكد.**

## 🔍 r2 (2026-09-23، «شوف كدا» — شغل لسه ماشي) — 🔁 Changes requested

**اتشغّل:** build 0 warnings · **`format` واقع** (whitespace في `AuthController.cs:79-90`، بقايا شيل الـ `try`) · `dotnet test` → **7 فشلوا / 7 عدّوا / 14**. كلهم نفس السبب: `JsonException: ... LoginResponseDto was missing required properties including: 'token', 'user'` — التيستات بتقرا الـ DTO على طول والـ login بيرجّع envelope.

**اتقفل من r1:** الـ routes (`api/v1`) · `return Ok(response)` · `catch (Exception)` في `/me` اتشال → **مفيش `catch (Exception)` في أي controller**. البلوكر 3 اتقفل، و2 نصه.

**🔴** الجزء (أ) لسه: التيستات لازم تقرا `data` من الـ envelope — ده شغل `ApiResponseAssert`. بعدها متوقع يقابل فخ الـ stub.
**🟡** `format` · كود متعلّق عليه لسه في `Register` (سطرين `CreatedAtAction` + بقايا الـ `catch`) · `[ProducesResponseType<ApiResponse<object>>(401)]` على `/me` بقى كذب (الرد ProblemDetails).
**🔵** مفيش newline في آخر `AuthController.cs` (التامنة).
**لسه متبدأش:** ب · ج · د (التيستات الجديدة).

**Unblock بعد r2 («وريني اعمل ايه في ملفات التيست») — مستوى 2:** signatures الـ helper (`SuccessAsync<T>` · `ProblemAsync`) + خريطة السطور اللي تتغيّر + تلميح إن الـ `tone` هيوقّع الـ deserialize (`JsonStringEnumConverter` مش في الـ web defaults). مفيش bodies. فخ الـ stub متقالش.

**«ساعدني» بعد الـ unblock — كود الجزء (أ) اتسلّم كامل في الدردشة.** اتجرّب قبل التسليم على نسخة في الـ scratchpad (ملفاته ماتلمستش): **15/15**، build 0 warnings. اللي اتسلّم: `ApiResponseAssert` · `AuthEndpointTests` كامل · `RegisterAndLoginViaApiAsync` + تيست النجاح الجديد · سطر الـ stub في `TranslyApiFactory` · `UserDto.From` بدل `mapper.Map` في `AuthService` + شيل `IMapper`.
- **فخ الـ stub اتأكد بالتشغيل** (5 × `InternalServerError`) واتقاله السبب ضمن الكود.
- **bug جديد اتكشف بالتيست الموجود:** `mapper.Map<UserDto>(user)` بيسيب `CreatedAt = 0001-01-01` لأن اسم الـ entity `CreatedAtUtc` — الـ login بيرجّع تاريخ غلط من ساعة ما AutoMapper اتضاف (`025d3c4`). `Login_ReturnsTheSameUserPayloadAsMe` هو اللي مسكه. AutoMapper بقى من غير أي استخدام (`AddAutoMapper` في `Program.cs` + الـ package) — القرار ليه.
- **↩️ رد: «انا عايز استخدم auto mapper»** → الخطوة 5 اتبدلت (قرار #28): `TranslyAI.Api/Mapping/UserProfile.cs` بـ `ForMember` للـ `CreatedAt` · `AddMaps` في `Program.cs` · `ProjectTo` في `GetProfileAsync` · `UserDto.From` اتشال · `ReverseMap` اتشال · تيست `TranslyAI.Api.Tests/Mapping/MappingConfigurationTests.cs`. متجرّب: **16/16**. **Break check:** شيل الـ `ForMember` → تيست الـ configuration بس اللي حمّر؛ `Login_ReturnsTheSameUserPayloadAsMe` فضل أخضر لأن الاتنين بقوا على نفس الـ mapping. اتقاله.
- **الجزء (أ) مايتحسبش دليل إنه يكتب ده لوحده.** الـ review هيحكم على اللي شغّله وغيّره.

## 📍 حالة 2026-09-23 بعد `680d39b` («عايز ابدأ تاسك» — فحص حالة، مش review)

**اتشغّل:** build `--no-incremental` 0 warnings · `dotnet test` → **14/15** · `format` واقع.
- **`Login_ReturnsTheSameUserPayloadAsMe` أحمر:** bug الـ `CreatedAt = 0001-01-01` لسه عايش. الكوميت فيه `AddAutoMapper` بس — `Mapping/UserProfile.cs` والـ `ForMember` وتيست `MappingConfigurationTests` (خطوة 5 بعد قرار #28) متطبقوش.
- **`format`:** whitespace في `TranslyApiFactory.cs:54-58`.
- الجزء (أ) متقفلش لسه بسبب الاتنين دول. ب · ج · د متبدأوش.
- اتعرض عليه: يكمّل 018 (الترشيح) ولا يقفلها عند (أ) وياخد اتجاه جديد بعد review للي اتعمل.
- **رده: «مش عايز اكتب تستات خلاص»** → اتعرض عليه 3 اختيارات (مفيش تيستات جديدة والموجودة تفضل خضرا [الترشيح] · التيستات في «ساعدني» · شيلها كلها). **مستني رده**، ولسه متسجّلش في `decisions.md`.
- **شاف الـ bug بنفسه في Postman** (`createdAt: 0001-01-01` في الـ login) وسأل «ازاي؟» → Explain: الـ mapping بالاسم (`CreatedAtUtc` ≠ `CreatedAt`)، و`required` مش بيحمي من الـ reflection، و`/me` سليم لأنه `Select` بالإيد، وتحويل `DateTime` Unspecified → `DateTimeOffset` بياخد offset السيرفر. اتعرض `AssertConfigurationIsValid()` وقت الـ startup كحارس من غير تيست.
- **«تمام اصلحت المشكلة» (working tree، مش متكوميت):** `ForMember` في `Program.cs:66-68` بـ `TimeSpan.Zero` → **15/15**، build 0 warnings. الحارس هو `Login_ReturnsTheSameUserPayloadAsMe` (بيقارن الـ mapping بالـ `Select` اليدوي في `/me`) — الكسر اتشاف فعلاً قبل التصليح. **🟡** `using Google.Protobuf.WellKnownTypes;` اتضاف في `Program.cs:1` (auto-import من dependency ترانزيتيف لـ `MySql.Data`، مش مستخدم، وفيه `Type`/`Enum`/`Timestamp` هيعملوا ambiguity). **🔵** `o=>` من غير مسافة. `format` لسه واقع في `TranslyApiFactory.cs:54-58`. `AssertConfigurationIsValid` متضافش (اختياري).

## 🪤 الفخاخ (للـ mentor — متتقالش قبل الـ review)

> بعد التعديل: فخ الـ `OnChallenge` وفخ `InvalidOperationException → 409` وفخ الـ `CS0162` مبقوش ليهم لازمة (الأولاني لأن `UseStatusCodePages` + `AddProblemDetails` اتعملوا صح، والتالت اتشال في r1). ترتيب الـ middleware اتعدّى في r1. الباقي قايم: الـ stub مش متوصّل · `ClientErrorResultFilter` (بقى في صالحه، `Unauthorized()` بترجع ProblemDetails لوحدها) · مفتاح `TargetLanguage`. **وفخ جديد:** في الـ Development، الـ developer exception page مع `AddProblemDetails` بترجّع ProblemDetails **فيها الـ exception والـ stack trace** — تيست الـ 500 بيعدّي في الـ `Testing` environment، بس لو حد شغّل الـ API بـ `Development` على سيرفر حقيقي هيسرّب.

- **الـ stub مش متوصّل (موجود في الكود من `4ccca52`، مش مزروع).** `Program.cs` بيسجّل `AddHttpClient<IGeminiApiService, GeminiApiService>` فاسم الـ typed client بيبقى `IGeminiApiService`. والـ factory بتظبط `AddHttpClient<GeminiApiService>()` واسمها `GeminiApiService`. النتيجة إن الـ primary handler بتاع الـ stub مش بيتنده، والتيستات بتكلم Gemini الحقيقي بـ `not-a-real-key` → `InvalidRequest` → 500 → `Translate_WhenTwoUsersSendTheSameText_...` بيحمرّ. **ده استنتاج من قراية الكود، متأكدتش منه بالتشغيل** (التيستات مكانتش بتـ compile). الـ ticket بيقول «ولا request يطلع لـ Gemini» ومبيقولش السبب. وده أول تمن حقيقي للـ interface اللي من غير قرار (#15).
- **`OnChallenge` + `HandleResponse()`** بيرجع قبل ما الـ handler يكتب الـ 401 والـ `WWW-Authenticate`، فالتيست الموجود بيحمرّ. `UseStatusCodePages` بيشتغل بعد ما الـ challenge يخلص فبيسيب الـ header.
- **`[ApiController]` بيحوّل `return Unauthorized()` لـ ProblemDetails** عن طريق `ClientErrorResultFilter`، يعني الرد فيه body، و`UseStatusCodePages` مش هيلمسه. تيست الـ login بباسورد غلط هو اللي بيكشفه. الحلول: `SuppressMapClientErrors` أو `IClientErrorFactory` مخصوص أو envelope صريح.
- **مفتاح `TargetLanguage` PascalCase** (دين TASK-006، `TranslationRequestDto.cs:27`). تيست الـ 400 بيقفله.
- **ترتيب الـ middleware:** `UseExceptionHandler` و`UseStatusCodePages` لازم ييجوا قبل `UseAuthentication`، وإلا الـ 401 بتاع الـ challenge هيعدّي من غير ما يتلف.
- **`InvalidOperationException` → 409** هو الحل السهل لأنها اللي `AuthService` بيرميها دلوقتي، والـ decision block بيقول ليه لأ.
- **7 × `CS0162`** في `TranslationsController` (الـ `return Problem(...)` اللي بعد كل `return`). الـ DoD بصفر warnings بيجبره يشيل الكود الميت، من غير ما يتقال.
