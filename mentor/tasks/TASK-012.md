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
