# 🎫 TASK-012 — أول مستخدم في الداتابيز: `User` + التسجيل + password hashing

**Milestone:** M5 — Auth · **الوقت المتوقع:** ~100–120 دقيقة · **الصعوبة:** ▓▓▓░░
**اتكتبت:** 2026-08-30 · **بطلب صريح منه** ("عايز ابدأ شغل، رأيك أبدأ بالـ auth والـ users؟")

---

## الهدف

`POST /v1/auth/register` بياخد إيميل وباسورد → بيسيب **صف واحد** في جدول `Users` جواه **hash** مش الباسورد، والتسجيل بنفس الإيميل تاني بيترفض.

بعد التاسك دي يبقى عندك أول identity حقيقية في المشروع — الحاجة اللي الـ history والاشتراكات والـ quota كلهم واقفين عليها.

## ليه دلوقتي

- ده أول نص في M5، والـ M5 هي **السبب اللي الباك اند اتبنى عشانه** (العدّاد والاشتراكات). كل اللي قبلها بيوفّر فلوس؛ ده اللي بيكسّب.
- **Register قبل login** لسبب واحد بسيط: مفيش حاجة تعمل عليها login. والـ JWT موضوع كامل لوحده — لو حطيناه هنا التاسك تبقى 4 ساعات ومش قابلة للمراجعة.
- **وميزة عملية:** التاسك دي **بتاكل صفر requests من Gemini**. الـ 20/يوم عايقاك من جلستين — هنا مش هيلمسوك.

## خارج الـ scope — متعملهاش دلوقتي

- ❌ Login وإصدار JWT
- ❌ `[Authorize]` وربط الترجمات/الـ history بالمستخدم
- ❌ Refresh tokens
- ❌ Email confirmation / forgot password / roles
- ❌ Rate limiting على الـ register (جاي في M6 — مقاومة الغريزة دي جزء من التاسك)
- ❌ ASP.NET Core Identity الكامل — اقرا الـ decision block تحت الأول

---

## المطلوب

### أ. الـ entity والـ migration

- [ ] `User` entity في `Entities/` — على نفس نمط `CachedTranslation.cs` (`required`، `[MaxLength]`، تسمية `...Utc` للوقت)
- [ ] `DbSet<User>` في `TranslyDbContext`
- [ ] الإيميل **unique على مستوى الداتابيز**، مش في الكود بس. عندك مثال شغال في [TranslyDbContext.cs:15](../../TranslyAI.Api/Data/TranslyDbContext.cs#L15)
- [ ] `dotnet ef migrations add` → **اقرا الملف المولّد قبل ما تطبّقه**، وقول لي في التسليم إيه اللي كنت متوقعه وإيه اللي لقيته
- [ ] `dotnet ef database update`
- [ ] **إثبات:** `SHOW INDEX FROM Users;` والصق الناتج

### ب. الـ hashing

- [ ] الباسورد مايتخزنش plaintext، **ومايتعملوش hash بإيدك**. مفيش `SHA256` ولا `MD5` ولا `salt` بتكتبه بنفسك
- [ ] استعمل `IPasswordHasher<User>` من `Microsoft.AspNetCore.Identity` — الـ decision block بيقول ليه دي بالذات مش غيرها
- [ ] سجّله في الـ DI
- [ ] **إثبات + سؤال:** الصق قيمة `PasswordHash` من الداتابيز، وقول لي **ليه بالطول ده** و**إيه اللي جواها غير الـ hash نفسه**

### ج. الـ endpoint

- [ ] `AuthController` فيه `POST /v1/auth/register`
- [ ] Request DTO فيه `email` + `password` بـ validation: الإيميل شكله إيميل، والباسورد له حد أدنى — **انت تقرر الحد وتكتب جنبه سطر بيقول ليه هو ده**
- [ ] Status code على النجاح — تختاره وتبرره
- [ ] **الـ response body: بصّ عليه بعينك في Postman قبل ما تقول خلصت**، وقرر هو المفروض يبقى فيه إيه. اكتب سببك
- [ ] التسجيل بإيميل موجود → status code تختاره وتبرره

### د. السؤال بالتجربة — إجباري، والإجابة من تشغيل مش من افتراض

سجّل `Mohammed@Test.com`. بعدين حاول تسجّل `mohammed@test.com`.

1. عدّى ولا اترفض؟
2. **ليه؟** — الإجابة **مش في كود C#**. شغّل `SHOW FULL COLUMNS FROM Users;` وبص على عمود `Collation`
3. لو عدّى: هل ده اللي انت عايزه لتطبيق فيه login؟
4. لو اترفض: هل انت **مضمون** إنه هيفضل يترفض لو المشروع اتنقل لـ PostgreSQL بكرة؟ ولو الإجابة لأ — يبقى السلوك ده جاي منين، ومين المسؤول عنه؟

> ده هو نفسه سؤال `"Hello"` vs `"hello"` المؤجل من TASK-010. بقى شغل حقيقي بدل تمرين.

---

## 🧭 قرار معماري: ASP.NET Core Identity ولا جدول `Users` بإيدك؟

- **اخترنا:** جدول `User` بإيدك + `IPasswordHasher<User>` من package الـ Identity **مستعملة لوحدها**
- **البدائل المرفوضة:** (1) Identity كامل بـ `IdentityDbContext` و`UserManager<T>`، (2) `BCrypt.Net-Next`، (3) hashing بإيدك

**ليه:**

- Identity الكامل بيجيب معاه **7 جداول** (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`) و~15 عمود على `IdentityUser` (`TwoFactorEnabled`, `LockoutEnd`, `SecurityStamp`, `PhoneNumberConfirmed`, `ConcurrencyStamp`…). مش هتلمس منهم حاجة في الشهور الجاية. ده **نفس منطق قرار #1 وقرار #15** بالظبط: مش هنجيب هيكل عشان يمكن نحتاجه.
- **بس الـ hashing نفسه حاجة ماتتكتبش بإيدك أبداً.** `PasswordHasher<T>` بيعمل PBKDF2-HMAC-SHA512 بـ 100,000 iteration، salt عشوائي **لكل مستخدم**، مقارنة fixed-time، و**رقم نسخة مخزّن جوه الـ string نفسه** عشان تقدر ترقّي الخوارزمية بعد سنتين من غير ما تكسر المستخدمين القدام. أربع حاجات، كل واحدة فيهم CVE لو غلطت فيها.
- يعني القرار هو: **خد الجزء الصعب اللي لازم يتعمل صح، سيب الجزء اللي مش محتاجه.**
- **✅ اتأكدت عملياً (2026-08-30): مفيش package تتسطب أصلاً.** `Microsoft.Extensions.Identity.Core.dll` شحنة مع الـ ASP.NET Core shared framework (`C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.11\`). يعني `using Microsoft.AspNetCore.Identity;` بيشتغل من غير أي سطر في الـ `.csproj`. اللي **مش** في الـ shared framework هو `Microsoft.AspNetCore.Identity.EntityFrameworkCore` و`Microsoft.AspNetCore.Identity.UI` — ودول بالظبط اللي بيجيبوا الـ 7 جداول. تكلفة القرار ده = **صفر**.

**إمتى كنا هناخد Identity كامل:** أول ما تحتاج **اتنين** من دول مع بعض — email confirmation، lockout بعد محاولات فاشلة، 2FA، external logins (Google/Apple)، إدارة roles. ساعتها الـ 7 جداول تبقى بتشتغل مش بتتفرج، والـ `UserManager` بيبقى بيوفّر شغل حقيقي.

**البديل التاني (BCrypt):** خيار محترم فعلاً وناس كتير بتفضّله. رفضناه لأنه package خارجية جديدة تعمل نفس اللي `Microsoft.AspNetCore.Identity` بتعمله، والتانية هتنزل عندك أصلاً في M5 لما نيجي على الـ JWT.

**الغلطة الشائعة (الاتجاهين):**
- ناس بتاخد Identity كامل "عشان الأمان" — والأمان اللي محتاجينه منه فعلاً = **class واحد**.
- وناس بتكتب `SHA256(password + salt)` وتفتكر إنها عملت حاجة. الـ SHA **سريعة بالتصميم** — GPU بيجرّب بلايين hash في الثانية. الـ password hashing المفروض يكون **بطيء بقصد**. الفرق ده هو الفرق بين تسريب داتابيز بيتفك في ساعة وتسريب بيتفك في سنين.

---

## مفاتيح تدور بيها

- `IPasswordHasher<TUser>` · `PasswordHasher<TUser>` · `PasswordVerificationResult` (هتلزمك التاسك الجاية)
- `HasIndex(...).IsUnique()` — مثال شغال في `TranslyDbContext.cs:15`
- `DbUpdateException` · `MySqlException` · MySQL error `1062`
- `[EmailAddress]` · `[StringLength]` / `[MinLength]`
- 📎 https://learn.microsoft.com/aspnet/core/security/data-protection/consumer-apis/password-hashing

---

## Definition of Done

1. `dotnet build --no-incremental` → **0 warnings**
2. `dotnet format --verify-no-changes` → نضيف
3. `dotnet test` → لسه **6/6** أخضر
4. `POST /v1/auth/register` بإيميل جديد → الـ status اللي اخترته
5. نفس الإيميل تاني → الـ status اللي اخترته للتكرار
6. `SELECT Id, Email, PasswordHash, CreatedAtUtc FROM Users;` → صف واحد، والـ `PasswordHash` مفهوش الباسورد
7. `SHOW INDEX FROM Users;` → unique index على `Email`

> ⚠️ الـ build بيقع بـ `MSB3027` لو التطبيق شغال. الحل: `dotnet build -p:BaseOutputPath=<مسار في مجلد مؤقت>`.

---

## 📋 شكل التسليم — عبّي الـ checklist دي في رسالة "خلصت"

```
□ أ.  entity + DbSet + unique index        → [الملف:السطر]
□ أ.  الـ migration اتقرت قبل التطبيق      → المتوقع: ____ / اللي حصل: ____
□ أ.  SHOW INDEX FROM Users;               → [الناتج ملصوق]
□ ب.  IPasswordHasher متسجّل في DI         → [الملف:السطر]
□ ب.  قيمة PasswordHash + ليه بالطول ده    → ____
□ ج.  الـ endpoint + الـ DTO + validation  → [الملف:السطر]
□ ج.  حد أدنى للباسورد + السبب             → ____
□ ج.  status code النجاح + السبب           → ____
□ ج.  شكل الـ response body + السبب         → ____
□ ج.  status code التكرار + السبب          → ____
□ د.  Mohammed@Test.com vs mohammed@test.com → عدّى/اترفض: ____ · السبب: ____ · على PostgreSQL: ____
□ DoD 1-7 كلهم عدّوا
```

**Definition of Submitted (القاعدة الثابتة):** قبل ما تقول "خلصت" — افتح الملف ده وعُدّ الـ acceptance criteria بند بند، وشغّل `dotnet format` و`git status`.


---

## 📜 السجل (اتنقل من progress.md في 2026-09-23 — النص زي ما هو)

| TASK-012 | `User` entity + `POST /v1/auth/register` + password hashing بـ `IPasswordHasher<User>` | M5 | ✅ Done (Approved r2) — `e22da7e` | **أهم حدث فيها مجاش من الكود:** صحّح كود الـ mentor **بالتشغيل** — الشرط `DbException { SqlState: "23000" }` عمره ما بيتحقق مع `MySql.Data` (`SqlState` = `null`)، وضاف `MySqlException { Number: 1062 }` من نفسه. اتأكدت بـ probe مستقل — هو صح. **الناقص كان التبليغ:** قال "تم" وبس. وقف كمان وسأل على `[EmailAddress]` على الـ entity **قبل** ما يشحنها — اعتراض استباقي، أول مرة. **⚠️ كل الكود اتسلّم له في رسالة "ساعدني"** — ماتتحسبش دليل على قدرته يكتب auth من الصفر. **شرط الـ approval (شيل الـ clause الميت) اتكوميت من غير ما يتنفّذ** → اتحوّل لبند (د) في TASK-013 |

- **اللي قبلها:** ✅ **TASK-012 اتقفلت واتكوميتت** — `e22da7e feat: add user registration with password hashing (TASK-012)`. الـ working tree نضيف.

### ⚠️ شرط الـ Approval بتاع TASK-012 اتكوميت من غير ما يتنفّذ (2026-08-30)

الـ review في r2 كانت **Approved بشرط** يشيل الـ clause الميت من [AuthService.cs:59](../TranslyAI.Api/Services/AuthService.cs#L59) (`DbException { SqlState: "23000" or "23505" }` — اللي **هو نفسه** أثبت إنه عمره ما بيتحقق مع `MySql.Data`). اتكوميت زي ما هو، ومعاه الـ nits (`IsUniqueConstrainViolation` ناقصة `t` · `"Gegister race lost"`).

**التصنيف:** مش نمط جديد ومش بلوكر — بس ده **بالظبط** بند 1 في "Definition of Submitted" (كل ملاحظة يا اتصلحت يا ليها سبب معلن). فيه حجة مقبولة محتملة (portability لـ PostgreSQL — Npgsql بيملا `SqlState` فعلاً)، بس الحجة دي **اتقالتش**. اتحطت كبند (د) في TASK-013: يتشال أو يتكتب سببه في تعليق.

**ماتتحسبش تكرار في المفكرة لحد ما يترد عليها** — الفرق بين "نسي" و"قرر وسكت" لسه مش معروف.

### 🔴🔴 تصحيح — الكود بتاعي كان غلط، وهو مسكه بالتشغيل (r2، 2026-08-30)

> **ده أهم حدث في الجلسة، وأهم من التاسك نفسها.**

الشرط اللي أنا كتبتهوله في رسالة "ساعدني":
```csharp
exception.InnerException is DbException { SqlState: "23000" or "23505" }
```
**عمره ما بيتحقق على الـ stack ده.** هو جرّبه (شال الـ `AnyAsync` زي ما اتطلب)، شاف إنه مش شغال، وضاف بنفسه:
```csharp
|| exception.InnerException is MySqlException { Number: 1062 }
```

**اتأكدت بنفسي بـ probe مستقل** (`MySql.Data 26.7.0` — نفس النسخة اللي المشروع بيحلّها، متسجّل في `scratchpad/probe`):

| | القيمة |
|---|---|
| النوع | `MySql.Data.MySqlClient.MySqlException` |
| `is DbException` | **True** |
| `SqlState` | **`null`** ⬅️ |
| `Number` | `1062` |
| `is DbException { SqlState: "23000" or "23505" }` | **False** |
| `is MySqlException { Number: 1062 }` | **True** |

**🔎 قاعدة تقنية تتسجّل ومتتنسيش:** MySQL **بيبعت** SQLSTATE `23000` فعلاً — شفته بعيني في الـ CLI (`ERROR 1062 (23000)`). بس **`MySql.Data` بيرث `DbException` ومبيملاش `SqlState` — بيسيبها `null`**. يعني النوع بيدّي واجهة قياسية والـ implementation سايبها فاضية، والـ compiler مش هيقولك. **الخطأ بتاعي كان إني استنتجت من سلوك السيرفر على سلوك الـ client library.** (Npgsql بيملاها فعلاً — فالفرق مش في المعيار، الفرق في المزوّد.)

**تقييم أدائه هنا:** ده **تاني مرة يصحّح للـ mentor بدليل** (الأولى: اكتشاف الـ Interactions API في 2026-08-25). والفرق إن دي **بالتشغيل مش بالقراية** — وده بالظبط الناتج اللي M7 اتقدّمت عشانه.

**اللي فضل ناقص — وهو في التبليغ مش في الشغل:** قال **"تم"** وبس. أهم معلومة في الجلسة (إن كود الـ mentor غلط) اتخبّت جوه commit صامت. **الشغل ممتاز، التقرير صفر.** ماتتحسبش نمط — بس تتقال صريح: نتيجة زي دي بتغيّر قرار معماري، ولازم تتقال.

### 🔍 نتيجة review TASK-012 r1 (2026-08-30)

**اتشغّل بنفسي:** `build --no-incremental` → **0 warnings / 0 errors** · `dotnet test` → **6/6** · `dotnet format --verify-no-changes` → نضيف (`Formatted 0 of 40`) · واتصلت بالـ MySQL بتاعته وفحصت الجدول والـ index والصف.

**✅ متحقق منه من الداتابيز مباشرة:**
- `IX_Users_Email` موجود و`Non_unique = 0` · `Id` = `char(36)` · `Email` = `varchar(254)` · `CreatedAtUtc` = `datetime(6)` بميكروثانية حقيقية (`.705202`)
- الصف: `01a053ad-614e-**7**b44-…` → **UUIDv7 فعلاً** (nibble النسخة = 7) يعني `Guid.CreateVersion7()` اشتغلت
- `PasswordHash` طوله **84** وبيبدأ `AQAAAAIAAY` → format v1 · PRF = 2 (HMACSHA512) · `0x000186A0` = **100,000 iteration**. يعني مسار الـ hashing حقيقي مش وهمي.
- **يعني DoD 4 (تسجيل بإيميل جديد) متحقق منه بالداتابيز** — مش محتاج إثبات إضافي منه.

**🧪 اللي جرّبته بنفسي بدل ما أطلبه منه (تطبيق لدرس TASK-008/009 — متلحش على تمرين نتيجته عندك):**
- `INSERT` مكرر على نفس الإيميل → `ERROR 1062 (23000)`. **الـ SQLSTATE = 23000 اتأكد** → نص الشرط `SqlState: "23000"` صح على مستوى السيرفر.
- `INSERT` بـ `MMD@TEST.COM` → **اترفض برضه**. يعني الـ collation `utf8mb4_0900_ai_ci` **case-insensitive** وبيرفض من غير ما يستنى الـ `ToLowerInvariant`. ده جواب الميكانيكا بتاعة بند (د)، واتقالله في الـ review.
- الصفوف فضلت **1** بعد التجربتين (الاتنين فشلوا) — صفر أثر على داتابيزته.

**🔴 البلوكر الوحيد:** مسار التكرار مالوش أي إثبات — لا 409 من الـ endpoint ولا إن الـ `catch (DbUpdateException) when (IsUniqueConstrainViolation)` بيشتغل أصلاً. **النص الناقص من المعادلة:** هل `MySql.Data` بيملا `DbException.SqlState`؟ لو لأ، الـ `when` ميتحققش والعميل ياخد **500 بدل 409**. أنا وصلت للحد اللي مينفعش أعديه من غير ما أشغّل تطبيقه — وده مقصود.

**🟡 `UserName`:** أضافه من نفسه (مكانش في الـ ticket) عبر 6 ملفات بشكل متسق — بس **من غير unique index ولا normalization**، والقيمة المخزّنة `mohammed adil` (فيها مسافة) يعني قصده display name. الاسم `UserName` في عرف .NET/Identity = الـ login handle الفريد، فالاسم بيوعد بضمانة الـ schema مش مديها. القرار المطلوب منه: `DisplayName` ولا handle حقيقي بـ index.

**🔵 Nits:** `IsUniqueConstrainViolation` (ناقصة `t`) · `"Gegister race lost"` في اللوج · `"Email Already registered."` · شال `[ProducesResponseType]` فالـ OpenAPI بقى بيقول 200 ومبيعرفش 201/409.

**⚠️ ملحوظة على التقييم (نفس ملحوظة TASK-010):** **كل الكود اتسلّم له في رسالة "ساعدني"** — الـ entity والـ service والـ controller والـ DTOs والـ DI. اللي كان قراره فعلاً: (1) شيل `[EmailAddress]` من الـ entity **بعد ما سأل عنها بنفسه قبل ما ينفّذ** — ودي المكسب الحقيقي في الجلسة، (2) إضافة `UserName` كاملة، (3) تشغيل الـ migration والتحقق. **ماتتحسبش دليل على قدرته يكتب auth من الصفر.**

### ✅ قرارات اتحسمت في جلسة 2026-08-30 (منه مباشرة)

| السؤال | الإجابة |
|---|---|
| **الـ auth بيحل مشكلة إيه؟** | **حساب حقيقي (email + password)** — مش anonymous device identity ولا الاتنين مع بعض. يعني `Users` table بإيميل وباسورد، والـ anonymous لو احتجناه بعدين يتبني فوقه |
| **التطبيق فيه auth دلوقتي؟** (سؤال intake #3) | ✅ **مفيش أي auth خالص.** بنصمم من الصفر على الجهتين، وإحنا اللي بنصدر الـ JWT ونتحقق منه. مفيش Firebase ولا Google/Apple Sign-In يتفاوض معاهم |
| **الـ Gemini billing** | 🟡 **لسه مفتوح — بس مش عايق دلوقتي.** M5 كلها بتاكل صفر requests من Gemini. الترقية تتأجل لحد أول تاسك تلمس الترجمة تاني |

### 🎫 TASK-012 — الملخص (الفخاخ المزروعة، تتراجع في الـ review)

- 🪤 **الـ uniqueness check اللي مش بيحرس اللي بيدّعيه.** الـ ticket بيطلب unique index في الداتابيز صراحةً (عشان النمط المسجّل 3 مرات)، **بس مش بيقول حاجة عن الـ race**: لو عمل `AnyAsync(...)` وبعده `SaveChanges`، فيه مسار بين الاتنين بيرمي `DbUpdateException` (MySQL 1062) → **500 عند العميل**. ده **نفس نمط "بيكتب دفاع مش قادر يغطي الحالة اللي بيدّعيها"** في مكان جديد.
- 🪤 **رجوع الـ EF entity في الـ response.** لو رجّع `User` → **`PasswordHash` بيروح للعميل**. الـ ticket مش بيقول ده؛ بيقول "بصّ على الـ response body بعينك وقرر يبقى فيه إيه" — يعني الفخ بيتفتح بس لو بصّ فعلاً. درس M2 (DTO ≠ entity) بيتحصد هنا.
- 🪤 **الـ collation** — بند (د). `Mohammed@Test.com` vs `mohammed@test.com`: MySQL بـ `utf8mb4_0900_ai_ci` هيرفض التاني، **والسلوك ده مش جاي من كوده خالص**. السؤال الحقيقي في البند: مين المسؤول عن السلوك ده، وهل هو مضمون على PostgreSQL. **ده سؤال TASK-010 المؤجل (`"Hello"` vs `"hello"`) اتحوّل لشغل حقيقي** — تطبيق للدرس المسجّل: حوّل البند المؤجل لشغل بدل ما تلح على تمرين مصطنع.

**اللي فضل صريح في الـ ticket بقصد:** إن الـ hashing ماينكتبش بإيده (خطر أمني حقيقي، مش مادة لفخ)، وإن الـ rate limiting على الـ register مؤجل لـ M6 (عشان ميضيّعش وقت فيه).

> **أسئلة معلّقة لسه محتاجة إجابة منه:**
> - ✅ ~~قرار #18~~ — اتقفل 2026-08-27 بدليل من الداتابيز، واتنفّذ في TASK-011.
> - ✅ ~~سؤال intake #3 (auth في التطبيق)~~ — اتقفل 2026-08-30: **مفيش أي auth**.
> - 🟡 **الـ billing** — الترقية لـ Tier 1 عملها ولا لأ؟ **مش عايق طول M5** (صفر Gemini)، بس بيرجع يبقى عايق أول تاسك تلمس الترجمة.
> - ⬜ الوقت المتاح أسبوعياً (سؤال intake رقم 4، لسه مجاوبش). **بقى ألزم دلوقتي** — M5 milestone من 5-6 تاسكات مش تاسكة واحدة.

- **🔺🔺🔺 صحّح كود الـ mentor بالتشغيل — أعلى نقطة وصلها (2026-08-30، TASK-012 r2).** الشرط `DbException { SqlState: "23000" }` اللي أنا كتبتهوله عمره ما بيتحقق مع `MySql.Data` (بيسيب `SqlState` = `null`). جرّبه، شافه بايظ، وضاف `MySqlException { Number: 1062 }` من نفسه. **اتأكدت بـ probe مستقل — هو صح وأنا غلط.**
  - **ليه دي أهم من اكتشاف الـ Interactions API:** ذاك كان **قراية** توثيق. ده **تشغيل**. وده حرفياً الناتج اللي M7 اتقدّمت من مكانها عشانه — النمط المسجّل 3 مرات ("بيكتب دفاع مش قادر يشتغل") اتقلب: بقى بيكتشف الدفاع الميت في كود غيره.
  - **الناقص = التبليغ.** قال "تم" وبس. تفصيلة زي دي بتغيّر قرار معماري ولازم تتقال، مش تتخبّى في commit.
- **🔺🔺 وقف وسأل على `[EmailAddress]` قبل ما يشحنها — الاعتراض الأول اللي بيسبق الخطأ (2026-08-30).** حط `[EmailAddress]` على `User.Email` (الـ entity) وسأل "ليه ماستخدمناهاش؟" بدل ما يسلّمها. **دي نفس بنية نمطه المسجّل 3 مرات** (`Validation` من غير `IValidatableObject` · الـ guard على `candidates` · `Unkown`/`Unknown`): attribute بيـ compile بـ 0 warnings ومحدش بينفّذه — EF Core مبيقراش validation attributes، والـ `Validator` بيشتغل على الموديل اللي اتعمله binding بس، والـ `User` عمره ما بييجي من الـ wire.
  - **الفرق الجوهري:** التلاتة اللي فاتوا اتمسكوا في الـ review **بعد** التسليم. دي اتمسكت **قبل** ما تتكتب. الخطوة اللي كانت ناقصة في النمط كله ("اجعل الحالة تحصل، وشوف الكود اشتغل") اتحوّلت لسؤال استباقي. **متتحسبش تكرار رابع** — دي الإشارة إن النمط بيتفكك.

