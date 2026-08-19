# 🎫 TASK-004 — `TranslationTone` enum: خلّي الحالة الغلط مستحيلة

**Milestone:** M2 — First real endpoints · **الوقت المتوقع:** ~75 دقيقة · **الصعوبة:** ▓▓▓░░
**اتفتحت:** 2026-08-19 · **الحالة:** 🚧 مفتوحة

---

## الهدف

دلوقتي الـ endpoint بتاعك بيقبل `"tone": "banana"` ويرجّع **200**. بيروح على الـ `default` في الـ switch، الـ `toneInstruction` بتبقى string فاضية، Gemini بيترجم من غير أي تعليمة نبرة، والـ response بيرجّع `"tone": "banana"` كأن كل حاجة تمام.

ده أسوأ نوع من الـ bugs: **مفيش error، والناتج غلط.** الـ client مش هيعرف إن النبرة اتجاهلت، وانت مش هتعرف إلا لما حد يشتكي إن الترجمة "مش عاملة حاجة".

بعد التاسك دي: قيمة نبرة غلط تترفض عند الحدود بـ **400 + ProblemDetails**، والكود جواه يبقى فيه ضمانة من الـ compiler إن كل نبرة موجودة ليها تعليمة.

## ليه دلوقتي

في TASK-002 كانت النبرة string وكانت مجرد حقل بيترجّع زي ما جه — مكانتش بتعمل حاجة، فالمشكلة كانت نظرية. **TASK-003 غيّرت ده:** النبرة بقت داخلة في الـ prompt فعلاً، يعني القيمة الغلط بقت بتغيّر الناتج بالفعل. الـ bug اتولد بالتاسك اللي فاتت.

وده كمان بيفتح أهم فرق بين Dart و C# في الـ enums — الفرق ده هيرجعلك في كل `switch` هتكتبه بعد كده.

## المطلوب

- [ ] `enum` اسمه `TranslationTone` بالقيم التلاتة: `Formal`, `Casual`, `Short` (بالـ PascalCase — دي convention .NET، الـ JSON حاجة تانية)
- [ ] `TranslationRequest.Tone` و `TranslationResponse.Tone` بقوا من النوع `TranslationTone` مش `string`
- [ ] الـ JSON جاي ورايح **lowercase**: التطبيق بيبعت `"tone": "casual"` والـ response بيرجّع `"tone": "casual"` — **مش** `"Casual"`. اتأكد من الاتنين بعينك في Postman، مش بالتخمين
- [ ] `"tone": "banana"` → **400** بـ `ProblemDetails` فيها اسم الحقل. من غير ما تكتب سطر validation واحد
- [ ] الـ request من غير حقل `tone` خالص لسه بيشتغل ويستعمل `Formal`
- [ ] الـ `switch` في `GeminiApiService` بقى **switch expression** مش switch statement، والـ arm بتاع القيمة غير المتوقعة **بيرمي** مش بيرجّع string فاضية — اقرا الـ decision block
- [ ] `TranslationsController.cs:18` — الـ 502 دلوقتي بيرجّع **string خام** مش ProblemDetails. الـ 400 بيرجّع JSON منظم والـ 502 بيرجّع نص. وحّدهم — الـ client عنده `sealed class Failure` واحدة بتعمل parse لشكل واحد
- [ ] `dotnet build` من غير أي warning جديد (بص على الـ Problems panel، مش على "Build succeeded")

## خارج الـ scope

- ❌ `GET /v1/languages` وvalidation إن `sourceLanguage` كود لغة صحيح → تاسك تانية
- ❌ FluentValidation أو أي validation library → M4. الـ نقطة هنا إن **النوع** هو الـ validation
- ❌ `IExceptionHandler` عامة → M4. الـ 502 دلوقتي يتصلح في مكانه
- ❌ الـ 🟡 المتبقية من TASK-003 (`finishReason`، `ValidateOnStart`، `Timeout`، تثبيت اسم الموديل) → تاسك التقوية الجاية
- ❌ إضافة نبرات جديدة. التلاتة دول هم اللي في التطبيق

## مفاتيح تدور بيها

- `JsonStringEnumConverter` — وتحديداً: بياخد `JsonNamingPolicy` في الـ constructor؟ وفين بتسجّله — global في `AddControllers().AddJsonOptions(...)` ولا `[JsonConverter]` attribute على الـ enum نفسه؟ (الاتنين شغالين — اختار وقول ليه)
- `switch` expression → https://learn.microsoft.com/dotnet/csharp/language-reference/operators/switch-expression
- الـ warning `CS8509` — اقراه كويس، هو نص الدرس
- `ControllerBase.Problem(...)` — وشوف الـ overloads بتاعته
- https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/converters-how-to

**سؤال تجاوب عليه بالتجربة مش بالقراية:** لو بعت `"tone": 1` (رقم مش string) — بيحصل ايه؟ جرّبها فعلاً وقول لي النتيجة في الـ submission. الإجابة دي هي اللي بتحدد إذا كان الـ `_` arm ضروري ولا لأ.

## Definition of Done

السيرفر شغال، والأربع طلبات دول بيدّوا النتايج دي:

| الـ body | المتوقع |
|---|---|
| `"tone": "casual"` | 200، والـ response فيه `"tone": "casual"` بالظبط كده |
| `"tone": "banana"` | **400** + ProblemDetails فيها اسم الحقل `tone` |
| الحقل `tone` مش موجود خالص | 200 بنبرة formal |
| Gemini واقع (شيل الـ key مؤقتاً) | **502** بـ `application/problem+json` مش `text/plain` |

وبعدين: `dotnet format` + `git status` نضيف + commit.

---

## 🧭 قرار معماري 19: enum بدل string للقيم المحدودة

- **اخترنا:** `enum TranslationTone` + `JsonStringEnumConverter`
- **البدائل:** string + `[RegularExpression]` attribute · string + `static class ToneNames` بـ constants · string زي دلوقتي + `if` في الـ service
- **ليه الـ enum:** الفرق مش في الـ validation — الفرق في **مين بيتحمّل المسؤولية**. مع الـ string، كل method بتاخد النبرة لازم تسأل نفسها "هي دي قيمة صالحة؟" وتقرر تعمل ايه لو لأ. مع الـ enum، السؤال ده بيتسأل **مرة واحدة عند الحدود** (الـ model binder)، وأي كود جوه الـ API بيشتغل على قيمة مضمونة. ده نفس المبدأ بتاع قرار #17 (`GeminiTranslationResult` مش nullable) — النوع اللي بيسمح بحالة ملهاش معنى بيجبر كل متصل يدافع عن نفسه.
- **إمتى كنت هتفضّل string:** لو القيم بتتحدد من الـ configuration أو الـ database ومش معروفة وقت الـ compile — مثلاً لو النبرات بقت feature يقدر الـ admin يضيف فيها. ساعتها الـ enum بيبقى قفل مش ضمانة، وأي نبرة جديدة تستلزم deploy.
- **الغلطة الشائعة:** إنك تعمل الـ enum وتسيب الـ validation القديمة مكانها "احتياطي". لو الـ binder ضمن القيمة، الفحص التاني كود ميت بيوهم القاري إن فيه حالة تانية ممكنة.

## 🧭 قرار معماري 20: الـ `_` arm — enums في C# **مش** exhaustive

دي أهم حاجة في التاسك دي، وهي فخ جاي ليك من Dart مباشرة.

في Dart، `switch` على enum الـ analyzer بيتأكد إنك غطّيت كل الحالات، وبعدها القيمة مش ممكن تكون حاجة تانية. **في C# مش كده.** الـ enum تحت السطح مجرد `int`، و `(TranslationTone)99` كود صالح بيـ compile من غير أي شكوى. الـ compiler هيديك warning (`CS8509`) إنك مغطتش كل الحالات، بس الـ warning ده بيتكلم عن الأعضاء المعروفين بس — مش عن الـ ints الغريبة.

- **اخترنا:** switch expression بتلات arms + `_` arm **بيرمي** exception
- **البدائل:** من غير `_` arm خالص (تعيش مع الـ CS8509) · `_ => ""` (نفس السلوك الحالي)
- **ليه بيرمي:** الـ binder هو الحارس بتاع المدخلات الخارجية. الـ `_` arm حارس مختلف تماماً — بيحرس من **الكود بتاعك انت**: cast غلط، أو عضو جديد تضيفه للـ enum بعد سنة وتنسى تضيف تعليمته. من غير الـ arm ده، النبرة الجديدة هتترجم بصمت من غير تعليمة — نفس الـ bug اللي بنقفله النهاردة، راجع بعد سنة ومحدش شايفه.
- **ليه مش `_ => ""`:** دي بالظبط المشكلة الحالية لابسة نوع جديد. الـ string الفاضية سلوك صالح في نظر الـ compiler وسلوك غلط في نظر المستخدم — وده تعريف الـ bug اللي بيعيش طويل.
- **إمتى الرمي يبقى غلط:** لو الـ `_` هتقع في مسار بيشوفه المستخدم النهائي. هنا مش كده — لو وقعت يبقى فيه bug في الكود، والـ 500 هو الرد الصحيح على bug في الكود.
- **الغلطة الشائعة:** إنك تسكّت `CS8509` بـ `_ => ""` عشان الـ build ينضف. **مفكرة الـ mentor مسجّل فيها إنك عملتها في TASK-003** (`CS8604` → خليت الـ record `string?`). الـ warning مش العدو — هو بيقولك إن فيه حالة مش مفكر فيها. الحل إنك تقرر تعمل ايه فيها، مش إنك تخفيها.

---

> 🔒 **Definition of Submitted** — قبل ما تقول "خلصت": (١) افتح آخر review واعدّ الملاحظات، (٢) افتح الملف ده واعدّ الـ acceptance criteria، (٣) `dotnet format` + `git status`.
