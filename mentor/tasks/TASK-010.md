# 🎫 TASK-010 — أول EF Core: نفس الترجمة ماتتشتراش مرتين

**Milestone:** M3 — Data Layer · **الوقت المتوقع:** ~120 دقيقة · **الصعوبة:** ▓▓▓▓░

---

## الهدف

نفس النص + نفس زوج اللغات + نفس النبرة → **مانكلّمش Gemini تاني**. الترجمة بتتحفظ في MySQL أول مرة، وبترجع من الداتابيز أي مرة بعد كده.

الـ `POST /v1/translations` شكل رده **مايتغيرش ولا حرف** — العميل مايعرفش إن فيه كاش أصلاً.

---

## ليه دلوقتي

**1. الـ quota.** الـ 20 request/يوم بقت عايق حقيقي في تلات جلسات ورا بعض. الكاش بيخلي أي نص جربته قبل كده **مجاني للأبد** — يعني التطوير اليومي بيبقى بيستهلك من الـ 20 بس أول مرة.

**2. ودي مش حيلة مؤقتة — دي المكسب الحقيقي في الإنتاج.** تطبيق ترجمة بيشوف نفس الجُمل مليون مرة ("مرحبا"، "شكراً"، "كيف حالك"). كل واحدة منهم بتتدفع مرة واحدة بدل كل مرة.

**3. ودي أول persistence حقيقي.** الـ history، والـ auth، وعدّاد الاشتراكات — كلهم واقفين على الجزء ده. أول ما `DbContext` يشتغل، التلاتة دول يبقوا شغل مباشر.

**4. صفر ارتباط بالـ Gemini billing.** التاسك دي بتقلّل نداءات Gemini مش بتزوّدها.

---

## الـ Stack — اللي اتحدد النهاردة

| | الاختيار | ليه |
|---|---|---|
| Database | **MySQL** | اختياره. الحاجة الجديدة هنا لازم تكون EF Core مش الداتابيز |
| Provider | **`MySql.EntityFrameworkCore` 10.0.9** (بتاعة Oracle) | ⚠️ **مش Pomelo.** Pomelo هي الأشهر وهي اللي في كل الـ tutorials، وآخر نسخة فيها **9.0.0** — مفيش نسخة لـ EF Core 10 والمشروع `net10.0` |
| Docker | **لأ، مش دلوقتي** | حاجة M8. انت محتاج database مش container |
| `dotnet ef` | **متسطبة عندك أصلاً — 10.0.9** | اتأكدت |

---

## المطلوب

### أ. الـ entity والـ `DbContext`

- [ ] entity `CachedTranslation` — الحقول اللي محتاجها عشان **تلاقي** الترجمة، والحقول اللي محتاجها عشان **ترجّعها**
- [ ] `DbContext` فيه `DbSet<CachedTranslation>`
- [ ] مسجّل في `Program.cs` بالـ connection string
- [ ] 🔴 **الـ connection string فيه password → user secrets، مش `appsettings.json`.** قرار #14 اتاخد على الـ Gemini key وهو نفسه ينطبق هنا حرفياً

> ⚠️ **دي entity مش DTO.** ليها identity وبتعيش في الداتابيز. `TranslationResponse` هي اللي بترجع للعميل، و`CachedTranslation` **عمرها ما تخرج من الـ service**. رجوع entity من endpoint = blocker في الـ review، مش ملاحظة.

### ب. الـ migration

- [ ] packages: `MySql.EntityFrameworkCore` + `Microsoft.EntityFrameworkCore.Design`
- [ ] `dotnet ef migrations add <اسم معبّر>`
- [ ] 🔴 **افتح الملف المولّد واقراه قبل ما تطبّقه.** وفي التسليم اكتب **سطرين** عن حاجة شفتها فيه مكنتش متوقعها
  - ده مش بند شكلي. الـ migration هو **الوحيد** في EF Core اللي بيتولّد ويتنفّذ على الداتابيز بتاعتك من غير ما حد يراجعه. عادة قراية الـ migration قبل تطبيقه هي اللي بتفرق بين مطور EF Core ومطور بيدعي
- [ ] `dotnet ef database update` والجداول اتعملت فعلاً
- [ ] ملف الـ migration **متكوميت** (مش في `.gitignore`)

### ج. المسار

- [ ] قبل ما نكلّم Gemini: دوّر في الداتابيز
- [ ] **لقيت** → رجّع من غير أي نداء لـ Gemini
- [ ] **ملقتش** → كلّم Gemini زي دلوقتي، واحفظ الناتج
- [ ] شكل الـ response زي ما هو بالظبط
- [ ] لوج بيقول إحنا جينا من الكاش ولا من Gemini — **محتاجه في البند (د)**

### د. الإثبات

- [ ] `dotnet run` + نفس الطلب **مرتين** → الأول فيه نداء Gemini، **التاني مفيهوش**. الصق اللوج
- [ ] نفس الطلب بالظبط بس **غيّر الـ `tone` بس** → **لازم** يكلّم Gemini تاني. الصق اللوج
  - لو مكلّمش، يبقى المفتاح ناقص وانت بترجّع ترجمة بنبرة غلط
- [ ] `SELECT` من الجدول ووريني الصفوف

---

## خارج الـ scope

- ❌ **`GET /v1/translations` (الـ history)** — محتاج تعرف الترجمة دي بتاعة مين، وده مالوش إجابة قبل الـ auth. M5.
- ❌ **انتهاء صلاحية الكاش / TTL** — قرار حقيقي، بس محتاج نعرف الأول هل الترجمة بتتغير أصلاً. بعدين.
- ❌ **repository pattern** — شوف الـ decision block التاني.
- ❌ **tests** — لأ. TASK-008 خلصت غرضها.
- ❌ **Docker / docker-compose** — M8.
- ❌ **عدّ الاستهلاك والـ quota** — بعد الـ auth.
- ❌ **الديون المؤجلة** (تسريب الـ charset، `TargetLanguage` PascalCase، `differnt`) — M4.

---

## مفاتيح تدور بيها

**الـ packages**
```
dotnet add package MySql.EntityFrameworkCore --version 10.0.9
dotnet add package Microsoft.EntityFrameworkCore.Design
```

**التسجيل**
- `builder.Services.AddDbContext<T>(options => options.UseMySQL(connectionString))`
- لاحظ الـ casing: `UseMySQL` مش `UseMySql` — دي بتاعة Oracle، وPomelo هي اللي بتسمّيها `UseMySql`. ده أول اختلاف هتقابله بين الاتنين

**الـ migrations**
- `dotnet ef migrations add <Name>` · `dotnet ef database update` · `dotnet ef migrations script` (بيوريك الـ SQL من غير ما ينفّذ)

**الاستعلام**
- `FirstOrDefaultAsync` · `AsNoTracking()` · `EF.Functions`
- 📎 [EF Core — Querying](https://learn.microsoft.com/en-us/ef/core/querying/)
- 📎 [Migrations overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

**الـ index**
- `modelBuilder.Entity<T>().HasIndex(...)` جوّه `OnModelCreating`
- ⚠️ MySQL/InnoDB عنده **حد أقصى لطول مفتاح الـ index = 3072 bytes**، و`utf8mb4` بياخد 4 bytes للحرف → يعني ~768 حرف. index على عمود نص طويل **هيرفض يتعمل**. ده مش تفصيلة — ده اللي بيحدد شكل المفتاح بتاعك

**اللوج بتاع الـ SQL**
- EF Core بيلوج كل استعلام على `Information`. لو مش شايفه، شوف `Logging:LogLevel` في `appsettings.Development.json`

---

## Definition of Done

```
dotnet run
```
ثم نفس الطلب مرتين → **الرد الثاني بيرجع من غير أي نداء لـ Gemini**، وبنفس شكل الرد الأول بالظبط.

وجنبها: `dotnet build` بـ **0 warnings** و `dotnet format --verify-no-changes` نضيف.

---

## 🧭 قرار معماري 1: مفتاح الكاش — أعمدة في الـ `WHERE`، ولا hash؟

- **البدائل:**
  - **أ.** `WHERE SourceText = @text AND SourceLanguage = @src AND TargetLanguage = @tgt AND Tone = @tone`
  - **ب.** عمود واحد `CacheKey` فيه hash للأربعة مع بعض، والـ `WHERE` عليه لوحده
- **الاختيار سايبهولك، بس لازم يعدّي من التلاتة دول:**
  1. **هل ينفع تعمل index عليه أصلاً؟** ارجع لحد الـ 3072 bytes فوق. نص الترجمة ممكن يكون فقرة كاملة
  2. **هل المقارنة بتفرّق بين حالة الحروف؟** الـ collation الافتراضي في MySQL (`utf8mb4_0900_ai_ci`) **case-insensitive و accent-insensitive**. يعني `"Hello"` و `"hello"` و `"héllo"` **نفس الصف** عند MySQL. ده اللي انت عايزه ولا لأ؟ والإجابة مختلفة تماماً لو المفتاح hash
  3. **إيه اللي بيحصل لو زوّدت حقل رابع للمفتاح بعد سنة؟** (مثلاً اسم الموديل)
- **الغلطة اللي بتكلّف فعلاً:** إنك تسيب الـ `tone` بره المفتاح. النص واحد واللغات واحدة، والنبرة مختلفة → **بترجّع للعميل ترجمة بنبرة مطلبهاش**. ودي مش هتظهر في أي error، الرد هيبقى 200 وسليم الشكل. عشان كده البند (د) فيه اختبار الـ tone صراحة

---

## 🧭 قرار معماري 2: repository ولا `DbContext` مباشرة؟

**هتحب تعمل `ITranslationRepository`.** جاي من Clean Architecture في الفلاتر، والـ instinct ده هيشتغل لوحده. متعملهاش، ودي الحجة:

- **`DbContext` هو أصلاً Unit of Work، و`DbSet<T>` هو أصلاً Repository.** ده مش تشبيه — دي حرفياً الـ pattern اللي EF Core متبني عليه. `ITranslationRepository` فوقه = repository حوالين repository.
- **الحجة المعتادة "عشان أقدر أعمل mock":** `DbContext` بيتعمله fake بالـ in-memory provider أو SQLite in-memory من غير أي interface.
- **الحجة التانية "عشان أغيّر الداتابيز بعدين":** ده اللي الـ provider بيعمله. غيّرت من MySQL لـ Postgres = سطر `UseMySQL` بيتغير.
- **ودي نفس منطق قرار #15 بالظبط** — الـ interface تتضاف أول ما يبقى فيها **مكسب**، مش استباقاً.
- **إمتى تبقى صح فعلاً:** لما يبقى فيه **منطق استعلام بيتكرر** في أكتر من مكان (مثلاً منطق "الترجمات المتاحة للمستخدم ده" اللي بيتعقّد بعد الـ auth والاشتراكات). ساعتها اللي بتستخرجه مش repository — ده **query object** أو extension method على `IQueryable<T>`، وده أصغر وأدق

**لو مقتنعتش:** اعمله، بس اكتب في التسليم **سطر واحد** بيقول المكسب الملموس. لو السطر ده اتكتب وأنا مقتنعتش، هنتناقش. لو مقدرتش تكتبه، يبقى الإجابة معروفة.

---

## 🪤 اللي بتراجع عليه في الـ review

1. **الـ `tone` مش في مفتاح الكاش** → ترجمة بنبرة غلط بـ 200. **دي correctness bug مش ملاحظة style.**
2. **`.ToListAsync()` قبل `.Where()`** — الفلترة بتحصل في الذاكرة بعد ما الجدول كله اتحمّل. على 20 صف مش هتحس؛ على 200 ألف الـ API بيقع. **اللي بيفرق: `IQueryable` لسه استعلام، `List` بقى بيانات.**
3. **`AsNoTracking()` ناقصة** على القراية — EF بيفضل ماسك snapshot لكل صف قراه عشان يقارن التغييرات. انت مش هتعدّل الصف ده، فده شغل ومساحة على الفاضي.
4. **الـ entity رجعت من الـ controller** بدل `TranslationResponse`.
5. **مفيش index على المفتاح** → كل ترجمة بتعمل table scan. اشتغل النهاردة، وقع بعد ٦ شهور.
6. **الـ connection string بالـ password في `appsettings.json`** → قرار #14.
7. **`DbContext` مسجّل `Singleton`** — `AddDbContext` بيسجّله `Scoped` افتراضياً لسبب. لو غيّرتها، اعرف انت بتعمل إيه.

---

## ❓ سؤال تجاوب عليه بالتجربة (مش بافتراض)

**قرار #4 بيقول `DateTimeOffset` مش `DateTime`. المشكلة: MySQL مالوش نوع بيخزّن الـ offset أصلاً.**

- بعد `migrations add`، افتح الملف المولّد: `CreatedAt` اتحوّلت لأنهي نوع في MySQL؟
- احفظ صف، وبعدين اقراه تاني من الداتابيز. **الـ offset اللي كتبته رجع زي ما هو؟**
- لو ضاع — إمتى بالظبط ضاع: وانت بتكتب، ولا وانت بتقرا؟
- والسؤال الأهم: **قرار #4 لسه صح؟** ولا `DateTimeOffset` كانت بتحمي من مشكلة بتترجع تاني عند حدود الداتابيز؟

مش هقولك الإجابة. بس دي أول مرة قرار معماري اتاخد بدري يقابل قيد حقيقي من طبقة تحتيه، وده بيحصل كتير.

---

## 📋 شكل التسليم

```
### أ. الـ entity والـ DbContext
- [ ] CachedTranslation                                    →
- [ ] DbContext + DbSet                                    →
- [ ] مسجّل في Program.cs                                   →
- [ ] connection string في user secrets                    →

### ب. الـ migration
- [ ] الـ packages                                         →
- [ ] migrations add                                       →
- [ ] سطرين عن حاجة في الملف المولّد فاجأتك                 →
- [ ] database update + الجداول اتعملت                     →
- [ ] الـ migration متكوميت                                →

### ج. المسار
- [ ] بيدوّر قبل Gemini                                    →
- [ ] لقيت → مفيش نداء                                     →
- [ ] ملقتش → نداء + حفظ                                   →
- [ ] شكل الـ response زي ما هو                             →

### د. الإثبات
- [ ] نفس الطلب مرتين — اللوج (الصقه)                      →
- [ ] غيّرت الـ tone بس — اللوج (الصقه)                     →
- [ ] SELECT من الجدول                                      →

### القرارات
- [ ] مفتاح الكاش: اخترت إيه وليه                          →
- [ ] repository: عملته ولا لأ، والسبب                     →

### السؤال
- [ ] DateTimeOffset في MySQL — إيه اللي حصل                →

### Definition of Submitted
- [ ] فتحت آخر رسالة review وعدّيت الملاحظات
- [ ] فتحت ملف التاسك ده وعدّيت الـ acceptance criteria
- [ ] dotnet format + git status
```


---

## 📜 السجل (اتنقل من progress.md في 2026-09-23 — النص زي ما هو)

| TASK-010 | أول EF Core — كاش الترجمات في MySQL: entity + DbContext + migration + المسار | M3 | ✅ **Done (Approved r2، الأربعة اتقفلوا r3)** | **r3 اتحقق بنفسي 2026-08-27:** `Design` بقت 10.0.9 · build **0 warnings / 0 errors** · `dotnet format` نضيف · `dotnet test` **6/6** (التيستات عدّت رغم إن `TranslationOutcome.Success` اتغيّر تحتها — لأنها بتـ assert على النوع والقيم مش بتبني الـ record). | **الشغل نفسه صح**: الـ migration مطابق للمتوقع حرف بحرف (`varchar(64)`+unique index · `Tone` بقت `varchar(16)` مش int · `longtext` · `datetime(6)`)، والـ `SELECT` أثبت مسار الكتابة بترجمة ar→en حقيقية بنبرة Casual، والميكروثانية (`.726522`) اتحفظت. **⚠️ ملحوظة على التقييم: معظم الكود اتسلّم له في رسالة "ساعدني"** — اللي كان قراره فعلاً: مكان الملفات (اختار صح) والتشغيل والتحقق. **r1 كان فيها 3 blockers ردّ عليهم كلهم بنيّة معلنة قبل الكوميت** (connection string، Timeout، الإثبات) — مقبولة، نفس منطق حجته المقبولة في TASK-003. **وأهم مخرج من التاسك مجاش من الكود — جه من الـ `SELECT`:** قرار #18 اتقفل واتقلب |

- ✅ TASK-010 اتقفلت واتكوميتت — `4e893d7 feat: cache Gemini translations in MySQL to cut API calls (TASK-010)`. الـ working tree نضيف.

> **⬜ لسه من TASK-010:** `"Hello"` vs `"hello"` (درس الـ collation)، وعدد الـ SQL statements في المرة التانية.

> **⬜ لسه من TASK-010:** `"Hello"` vs `"hello"` (درس الـ collation)، وعدد الـ SQL statements في المرة التانية.

> **⚠️ ملحوظة على الـ build:** الـ app بتاعه بيفضل شغال وبيقفل ملفات `bin/` — الـ build بيقع بـ MSB3027. الحل من غير ما تقفل عليه عمليته: `dotnet build -p:BaseOutputPath=<مسار في الـ scratchpad>`.

> **أسئلة تجريبية من TASK-010 لسه مجاوبش عليها:** `"Hello"` vs `"hello"` (نداء واحد ولا اتنين — درس الـ collation)، وعدد الـ SQL statements في المرة التانية.

- **🔺🔺 تحققه العملي أنتج المعلومة اللي قفلت قرار معلّق — المرة التانية (2026-08-27).** لصق صف واحد من `SELECT` عشان يثبت إن الكاش شغال، والصف ده هو اللي **قفل قرار #18** (الـ `Model` جه بالـ alias مش بالاسم المحلول) — حاجة كانت معلّقة من TASK-007 وكنت بستناها منه كسؤال. **الدرس:** أهم مخرج من التاسك مجاش من الكود، جه من إنه بص على البيانات الحقيقية. **يتقال له صراحة** — ده بيربط "أثبت اللي عملته" بمكسب ملموس بدل ما تفضل إجراء.
  - **ملحوظة على التقييم:** معظم كود TASK-010 اتسلّم له في رسالة "ساعدني". اللي كان قراره فعلاً: مكان الملفات (اختار صح) + التشغيل + التحقق. **متتحسبش التاسك دي كدليل على قدرته يكتب EF Core من الصفر** — ده يتقاس في تاسك جاية.

