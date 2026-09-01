# 🎫 TASK-015 — أول integration tests: الـ API كله شغّال في التيست، وفوترة الترجمة عليها حارس

**Milestone:** M7 — Testing (راجعة بسبب حقيقي)  ·  **الوقت المتوقع:** ~2 ساعات  ·  **الصعوبة:** ▓▓▓▓░

---

## الهدف

تلات حقائق عن `POST /v1/translations` تبقى **مثبتة بكود بيشتغل لوحده** بدل ما تتثبت بـ curl بالإيد كل مرة:

1. الـ endpoint **مقفول** — من غير توكن مفيش ترجمة.
2. **مستخدمين مختلفين على نفس النص** = صف كاش **واحد** + صفين استخدام + نداء واحد لـ Gemini.
3. **فشل من Gemini = صفر فوترة** — ماينفعش نسجّل استخدام على حاجة مانجحتش.

بعد التاسك دي، `dotnet test` لوحده بيجاوب على التلاتة دول.

---

## ليه دلوقتي

سببين، والاتنين اتولدوا من TASK-014 نفسها:

**1. الفوترة بقت الحاجة اللي عليها فلوس، ومالهاش أي حارس آلي.**
في [TranslationService.cs](../../TranslyAI.Api/Services/TranslationService.cs) ترتيب `RecordUsageAsync` قبل `CachedTranslations.Add` هو اللي بيمنع صف الفوترة إنه يضيع مع فشل الكاش. النهارده: اقلب السطرين → بيـ compile، الستة تيستات خضرا، الـ endpoint بيرجّع 200.

وانت رفضت التعليق كحارس — **وكنت صح**. رفضك هو الإجابة على سؤالي: التعليق بيتقري، والتيست بيحمرّ. دي التاسك اللي بتحط الحارس الحقيقي.

**2. طلبت منك curl و`SELECT` بالإيد على سبع تاسكات.**
ده شغل ممل، مش قابل للتكرار، وبيضيع مع أول قفلة terminal. العيب في تصميم التاسك عندي مش في انضباطك — والتصليح إن الإثبات يبقى **شغل حقيقي في الريبو** مش إجراء بتعمله كل مرة.

والحتة الحلوة: الـ seam اللي بنيته في TASK-008 (`StubHttpMessageHandler`) هو **بالظبط** اللي هيخلّي التيستات دي تعدّي من غير ما تاكل من quota الـ Gemini بتاعتك.

---

## المطلوب

### أ) البنية التحتية

- [ ] `WebApplicationFactory` بيرفع الـ API الحقيقي (نفس `Program.cs`، نفس الـ middleware pipeline، نفس الـ controllers) جوه مشروع `TranslyAI.Api.Tests`
- [ ] الـ factory بيوجّه الـ `TranslyDbContext` على **داتابيز تيست منفصلة** — مش الداتابيز اللي بتشتغل عليها في الـ development
- [ ] الـ schema بتاع داتابيز التيست بيتبني من **الـ migrations الحقيقية** بتاعة المشروع
- [ ] الـ `GeminiApiService` بيتنادى من غير ما يخرج على الشبكة — الرد بيتحدد من التيست
- [ ] **صفر credentials متكوميتة.** connection string وsigning key وapi key بتاعت التيستات: انت تختار الآلية، بس محصلتها إن `git status` نضيف
- [ ] **صفر تعديل في `TranslyAI.Api/`** — لو اضطريت تلمس سطر فيه، ده مقبول بس **قوله ليه** (فيه سطر واحد على الأقل هتحتاجه، والسبب مش عيب فيك)

### ب) التيستات التلاتة

- [ ] **تيست 1 — مقفول:** `POST /v1/translations` من غير `Authorization` header → **401**
- [ ] **تيست 2 — مستخدمين مختلفين / نفس النص:**
  - مستخدم A يترجم نص، بعدين مستخدم B يترجم **نفس** النص بنفس الزوج ونفس النبرة
  - الـ assertions: `CachedTranslations` فيها **صف واحد** · `TranslationUsages` فيها **صفين** · الـ `Source` بتاعهم `Gemini` و`Cache` · **الـ Gemini stub اتنادى مرة واحدة بالظبط**
- [ ] **تيست 3 — فشل Gemini:** الـ stub يرجّع 500 → الرد **502** و`TranslationUsages` فيها **صفر** صفوف

### ج) شروط على التيستات نفسها

- [ ] `dotnet test` مرتين ورا بعض من غير ما تلمس حاجة → **الاتنين خضرا**
- [ ] التيستات التلاتة تعدّي من **داتابيز فاضية تماماً** (امسح الـ schema بالكامل وشغّل)
- [ ] `dotnet build --no-incremental` بصفر warnings · `dotnet format --verify-no-changes` نضيف
- [ ] الستة تيستات القديمة لسه خضرا (المجموع 9)

---

## خارج الـ scope

- ❌ **Testcontainers / Docker** — الأداة الصح على المدى الطويل، وموعدها M8 مع الـ CI. النهارده MySQL اللي عندك كفاية
- ❌ تيستات على `/v1/auth/*` — تسجيل الدخول هيتنادى عشان تجيب توكن، بس مش هو الحاجة اللي بتتختبر
- ❌ تيست على الـ cascade delete — اتحقق منه بالتشغيل في TASK-014 وهو config مش سلوك
- ❌ أي refactor في `TranslationService` — دي تاسك بتحط حارس على الكود، مش بتغيّره
- ❌ الـ quota والـ 429 — تاسك لوحدها، وبتيجي بعد دي بسبب
- ❌ `dotnet test` في GitHub Actions — M8

---

## 🧭 قرار معماري: أنهي داتابيز في التيستات؟

- **اخترنا:** **MySQL حقيقية، schema منفصلة** (مثلاً `translyai_test`) + الـ migrations بتتطبق عليها
- **البدائل:** `Microsoft.EntityFrameworkCore.InMemory` · SQLite in-memory · Testcontainers
- **ليه MySQL الحقيقية هنا:** الكلمة المهمة في اسم التاسك هي **integration**. القيمة كلها إن الـ provider الحقيقي والـ migrations الحقيقية والـ `varchar(64)` والـ unique index والـ `datetime(6)` كلهم شغالين. لو غيّرت الـ provider، التيست بيبقى بيختبر **كود EF Core** مش **الداتابيز بتاعتك** — والحاجة اللي بتقع في الإنتاج هي التانية.
- **ليه InMemory غلط تحديداً:** هو مش relational أصلاً. مبيعرفش unique index، مبيعرفش FK، مبيعرفش cascade، ومبيرفضش صف بيكسر constraint. يعني تيست 2 ممكن **يعدّي أخضر وهو مش بيثبت أي حاجة** عن الجدول الحقيقي. ودي أخطر من تيست بيحمرّ. **الفريق بتاع EF Core نفسه بيقول متستخدموش في الحتة دي** — الرابط تحت.
- **إمتى SQLite تبقى صح:** لما تكون بتختبر منطق LINQ/mapping ومش فارق معاك تفاصيل الـ provider. هنا فارق.
- **إمتى Testcontainers تبقى الصح:** لما التيستات تحتاج تشتغل على جهاز مش جهازك — CI. وده M8 بالظبط، ولما ييجي هيبقى تغيير في سطر واحد لأن الـ factory هيكون مبني صح.
- **الغلطة الشائعة:** أول نتيجة في جوجل لـ "EF Core unit test" هي InMemory، وهي الأسهل والأسرع، وهي اللي بتخلّي حد يقول "عندي تيستات" وهو مش عنده.

---

## 🧭 قرار تاني: التوكن في التيست — نجيبه ولا نصنّعه؟

مش هقولك الإجابة، بس خد المعيار: التيست اللي بيصنّع توكن بإيده بيفترض إن الـ `JwtTokenService` صح؛ التيست اللي بيعدّي على `register` + `login` بيثبت المسار كله. **الاتنين مقبولين هنا** — بس اللي تختاره **يبان في اسم الـ helper** بحيث اللي بعدك يعرف إحنا بنفترض إيه.

---

## مفاتيح تدور بيها

**الأساسي:**
- [Integration tests in ASP.NET Core](https://learn.microsoft.com/aspnet/core/test/integration-tests) — الصفحة دي فيها 80% من التاسك
- [Testing EF Core applications](https://learn.microsoft.com/ef/core/testing/) — وبالذات صفحة "Testing without your production database system" وهي اللي فيها حجة الـ InMemory

**كلمات:**
- `WebApplicationFactory<TEntryPoint>` · `IClassFixture<T>` · `WithWebHostBuilder` · `ConfigureTestServices`
- **`WebApplicationFactory` + top-level statements** — أول حاجة هتقابلك وهتاخد 10 دقايق، مش أكتر. الكلمة المفتاحية: الـ `Program` class المولّدة من الـ top-level statements **مش `public`**
- `ConfigureAppConfiguration` / `UseSetting` — الـ host بتاع التيست **مش** بيقرا user secrets بتاعة مشروع الـ API زي ما انت متوقع. اتوقّع تظبط الـ configuration بإيدك، وده الصح مش الـ workaround
- `ConfigurePrimaryHttpMessageHandler` — الحتة اللي بتوصل `StubHttpMessageHandler` بتاعك بالـ typed client المتسجّل بـ `AddHttpClient<GeminiApiService>`
- `Database.Migrate()` · `EnsureDeleted()`
- `UserSecretsId` في `.csproj` بتاع مشروع تيست

**حاجة تنتبه لها:** `StubHttpMessageHandler` الحالي بيرجّع **نفس** الـ `HttpResponseMessage` instance في كل نداء. لو حصل نداءين، التاني بيقرا stream متقري خلاص. تيست 2 محتاج يعرف **عدد** النداءات أصلاً — فكّر في ده وانت بتوسّعه.

**ملحوظة build:** لو الـ API شغّالة عندك، الـ build بيقع بـ `MSB3027` (ملفات `bin/` مقفولة). اقفل السيرفر أو استخدم `-p:BaseOutputPath=`.

---

## 🧪 أسئلة تجاوب عليها بالتشغيل

1. بعد أول run ناجح، افتح schema التيست وبص في جدول `__EFMigrationsHistory` — **فيه كام صف، والـ migration الفاضية `20260831142943_EditTranslationUsage` موجودة فيهم؟**
2. في تيست 3، `CachedTranslations` هي كمان صفر. **أنهي سطر بالظبط في `TranslationService` هو اللي ضامن ده؟** (سطر واحد، وهو موجود قبل التاسك دي)
3. لو **نفس** المستخدم بعت **نفس** النص مرتين — `TranslationUsages` فيها كام صف؟ وهل ده الصح من ناحية البيزنس؟ (جاوب بجملة، مش بتيست)

---

## Definition of Done

```
dotnet test
```

→ **9/9 أخضر**، وبيعدّي كمان لو مسحت schema التيست بالكامل قبله.

---

## ازاي الـ review هتشتغل — عشان تعرف "التيست الكويس" معناه إيه

الـ review مش هتقرا التيستات بس. **هكسر سطور في `TranslyAI.Api/` بنفسي وأشغّل تيستاتك انت.** الحاجات اللي هجرّبها:

- أشيل `[Authorize]` من `TranslationsController`
- أقلب `RecordUsageAsync` تحت `CachedTranslations.Add`
- أخلّي الكاش يتفلتر بالمستخدم كمان

**تيست بيفضل أخضر والكود مكسور = finding** — ده حرفياً نفس النمط المسجّل عليك ٣ مرات ("دفاع مش قادر يشتغل")، بس في مكان جديد. والفرق إن المرة دي **أنا** اللي بشغّل الحالة، مش انت اللي بتوصف إنك شغّلتها.

**ولو الوقت مكفّاش:** البنية التحتية لوحدها ممكن تاكل الجلسة. لو وصلت لتيست 1 شغّال (401 من `WebApplicationFactory` حقيقي على داتابيز حقيقية) ووقفت — **دي تسليمة مقبولة**، قول كده وخلاص. أصعب حتة في التاسك دي هي أول تيست، والتالت بيتكتب في 15 دقيقة بعده.
