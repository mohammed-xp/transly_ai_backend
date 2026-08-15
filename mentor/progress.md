# .NET Mentor — Progress

> ملف الحالة بتاع الـ mentor. بيتقرا في أول كل جلسة وبيتحدث في آخرها.
> مكانه: `mentor/progress.md` في الريبو بتاع الباك اند.

**آخر تحديث:** 2026-08-15

---

## المشروع

- **التطبيق:** Transly AI — تطبيق Flutter لترجمة النصوص بالـ AI
- **الـ slice المتفق عليه:** الترجمة end-to-end من خلال الباك اند — التطبيق يبعت النص واللغة للـ API، والـ API هو اللي بيكلم Gemini ويرجع الترجمة. الـ Gemini key يقعد على السيرفر بس.
- **باك اند موجود قبل كده؟** لأ — التطبيق بينادي Gemini API مباشرة من الـ client (وده السبب الأساسي للمشروع ده)
- **مصدر الـ API contract:** ⏳ في انتظار مكان ريبو الـ Flutter — المطلوب منه: الـ models بتاعة الترجمة + الـ service/repository اللي بينادي Gemini

## الـ Stack المتفق عليه

| العنصر | الاختيار | ليه |
|---|---|---|
| .NET | 10 (net10.0) | LTS، ومتسطب عنده أصلاً (SDK 10.0.302) والمشروع متظبط عليه |
| API style | Controllers | الـ API رايح على auth + subscriptions + admin، والـ controllers بتستوعب النمو ده أحسن من minimal APIs |
| Database | ⏳ PostgreSQL (مبدئي — يتأكد في M3) | مجاني، بيشتغل في Docker، وأرخص hosting من SQL Server |
| ORM | EF Core | الـ default في الـ ecosystem، وبيدي code-first migrations |
| Auth | ⏳ JWT (التفاصيل في M5) | متوقف على إذا التطبيق فيه Firebase Auth ولا لأ |
| Testing | xUnit + NSubstitute | الـ default في .NET |
| Deployment | ⏳ Docker + GitHub Actions (M8) | يتحدد الـ host وقتها |

## الوضع الحالي

- **Milestone:** M0 — Intake & setup
- **التاسك المفتوحة:** TASK-001 — تنضيف المشروع و `/health` endpoint
- **الوقت المتاح أسبوعياً:** ⏳ في انتظار الرد

## التاسكات

| ID | العنوان | Milestone | الحالة | نتيجة الـ review |
|---|---|---|---|---|
| TASK-001 | تنضيف الـ template + `/health` + أول commit | M0 | 🚧 In progress | — |

## قرارات معمارية اتاخدت

| # | القرار | اخترنا | البديل المرفوض | السبب باختصار |
|---|---|---|---|---|
| 1 | شكل الـ solution | مشروع واحد (`TranslyAI.Api`) | 4 projects: Api/Application/Domain/Infrastructure | الـ API لسه endpoint أو اتنين — الـ layering دلوقتي ceremony مش testability. نقسّم لما يبقى فيه سبب حقيقي |
| 2 | API style | Controllers | Minimal APIs | الـ API رايح على auth + subscriptions، والـ controllers بتديه attribute routing و `[Authorize]` و model validation في مكان واحد |
| 3 | مكان الـ Gemini key | على السيرفر بس | obfuscation أو تشفير الـ key في الـ APK | أي key بيوصل للـ device بيتسرب — decompile أو HTTPS proxy. مفيش حل client-side |

## مفكرة الـ mentor — نقاط بتتكرر

- [ ] ميل واضح لنقل Clean Architecture من الـ Flutter كما هي (عمل 3 فولدرات layers قبل ما يكتب endpoint واحد) — نراقبها في كل review

## نقاط قوة

- Senior Flutter — DI، layering، async، REST، JSON، auth flows، CI/CD كلها مفاهيم مفهومة. السينتاكس بس الجديد.
- عنده تمارين .NET قديمة في `source/repos` (HR.LeaveManagement, BookStoreApp, MyFirstApi) — يتأكد وصل فيها لفين

## الجلسة الجاية

review لـ TASK-001، وتحديث الملف ده بإجابات الـ intake (مكان ريبو الفلاتر، مستوى الـ C#/EF Core، الـ auth الحالي، الوقت الأسبوعي).
