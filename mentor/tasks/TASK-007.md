# 🎫 TASK-007 — هجرة الـ AI proxy لـ Gemini Interactions API

**Milestone:** AI proxy · **الوقت المتوقع:** ~120 دقيقة · **الصعوبة:** ▓▓▓▓░
**اتكتبت:** 2026-08-25 · **مصدر التاسك:** هو (اكتشف بنفسه إن `generateContent` مبقتش المستحسنة)

---

## الهدف

`POST /v1/translations` يرجّع **ترجمة حقيقية من الـ Interactions API**، وتصنيف الفشل بتاع TASK-005 يفضل صحيح — لكن مبني على **مفردات الـ API الجديدة**، مش على ترجمة حرفية للقديمة.

---

## ليه دلوقتي

إنت لقيت إن Google بتقول بالنص إن الـ Interactions API هي الـ recommended standard primitive لأي مشروع جديد، وإن `generateContent` بقت للتكاملات القايمة. إحنا مشروع جديد.

**بس الأهم من "الجديد أحسن":** جرّبت تغيّر الـ endpoint والـ request بس، وسيبت قراءة الرد زي ما هي. النتيجة كانت هتبقى **502 على كل ترجمة ناجحة، في صمت** — لأن الـ DTO بيدور على `candidates` والرد فيه `steps`، وكل الحقول nullable (قرار #22) فمفيش exception يتولد.

الدرس اللي التاسك دي مبنية عليه: **تغيير API مش تغيير endpoint. هو تغيير الـ contract في الاتجاهين.**

---

## اللي اتأكدت منه من التوثيق (عشان توفر وقت، مش عشان تنقل)

| | الجديد |
|---|---|
| الطلب | `{ "model": "...", "input": "..." }` — الـ `model` بقى في جسم الطلب مش في الـ path |
| الرد — الجذر | `id` · `status` · `model` · `steps[]` · `usage` · `errors[]` |
| `status` enum | `queued` · `in_progress` · `requires_action` · `completed` · `failed` · `cancelled` · `incomplete` · `budget_exceeded` |
| `steps[]` | array فيه أنواع مختلفة — `model_output` · `function_call` · `user_input` |
| النص | جوه step نوعه `model_output`، في `content[]` كعنصر `{ "type": "text", "text": "..." }` |
| `usage` | snake_case (`total_input_tokens` …) |

الباقي **إنت تدوّر عليه** — مش هنقل لك التوثيق.

---

## المطلوب

### الجزء أ — الطلب (~25د)

- [ ] `GeminiRequestDto` يتحوّل لشكل الـ Interactions. الـ record بيتغيّر، **مش** بيتبدل بـ anonymous object.
- [ ] الـ DTOs القديمة اللي مبقاش ليها لازمة (`GeminiRequestContent`, `GeminiRequestPart`) **تتمسح**. مش تتعلّق عليها comment. الـ git هو الـ history.
- [ ] الـ URL في `PostAsJsonAsync` يتظبط. ⚠️ **اتأكد من الـ version prefix الصح** — متفترضش `v1beta`.

### الجزء ب — الرد (~45د)

- [ ] DTO جديد لشكل الـ Interaction. نفس قاعدة قرار #22: **كل الحقول nullable**، لأنه رد مزوّد خارجي مش مدخل من عندنا.
- [ ] استخرج النص المترجم.
  ⚠️ `steps` **مش زي `candidates`**. الـ `candidates` كانت array من نوع واحد؛ الـ `steps` array **مختلطة الأنواع بحكم التصميم** — ممكن يبقى فيها thoughts وtool calls. `steps[0]` مش بالضرورة هو الترجمة. اختار بالـ **نوع**، مش بالموضع.
- [ ] لو مفيش step نوعه `model_output` فيه نص → دي حالة فشل، مش نص فاضي.

### الجزء ج — تصنيف الفشل (~40د)

الجزء ده هو التاسك الحقيقية. الـ `TranslationOutcome` hierarchy بتاعت TASK-005 **تفضل زي ما هي** — اللي بيتغير هو **مصدر المعلومة** اللي بتقرر أي outcome.

- [ ] `GeminiFinishReason` + `MapFinishReason` مبقاش ليهم مصدر (`finishReason` مش موجود). يتبدلوا بحاجة مبنية على الـ `status` الجديد.
- [ ] **اقعد اعمل الجدول ده بنفسك وحطه في التسليم:**

  | `status` | يروح على أنهي `TranslationOutcome` | و ليه |
  |---|---|---|
  | `completed` | | |
  | `failed` | | |
  | `incomplete` | | |
  | `budget_exceeded` | | |
  | `queued` / `in_progress` | | |
  | `requires_action` | | |
  | `cancelled` | | |

- [ ] **تلات حالات مالهمش مقابل في الـ API القديمة — خد بالك منهم:**
  1. الـ HTTP بيرجّع **200** والـ `status` مش `completed`. ده **مكنش ممكن يحصل** في `generateContent`. لو كودك بيقول "200 يبقى فيه ترجمة"، ده اللي هيكسره.
  2. `budget_exceeded` — حالة **جديدة خالص**. فكّر: دي بتاعت مين؟ وبتيجي في الـ body مش في الـ HTTP status، فالفحص القديم على `TooManyRequests` مش هيشوفها.
  3. `errors[]` array على الجذر — رد 200 وفيه أخطاء مسجّلة. لو تجاهلتها، بتترمي معلومة تشخيصية.
- [ ] **`SAFETY` / `RECITATION` → 422** كانت الحالة الوحيدة اللي العميل يقدر يتصرف فيها. دوّر: الحالة دي بتتقال إزاي دلوقتي؟ لو مش لاقيها، **قول إنك مش لاقيها** بدل ما تخمّن.

### الجزء د — قرار #18 (~10د)

الرد فيه حقل `model`. القرار #18 اتاخد أصلاً على أساس تحقق عملي: الـ config قالت `gemini-flash-latest` والرد جه `gemini-3.7-flash`.

- [ ] اتحقق **بنفس الطريقة**: هل الـ `model` الراجع هو الـ alias اللي بعتّه ولا الاسم المحلول؟
- [ ] لو رجع الـ alias زي ما هو → قرار #18 **مبقاش قابل للتنفيذ** بالشكل ده، ولازم يتعاد فتحه في التسليم.

---

## 🧭 قرار معماري مطلوب منك: التخزين

الـ Interactions API **بتخزّن الـ interactions على سيرفرات Google افتراضياً** — دي مذكورة كميزة (server-side state management للمحادثات متعددة الأدوار).

إحنا بنترجم **نصوص المستخدمين**.

- [ ] دوّر: فيه parameter بيقفل التخزين؟ إيه الافتراضي؟
- [ ] قرر واكتب السبب. الأسئلة اللي تجاوب عليها:
  - إحنا محتاجين state بين الطلبات؟ (الترجمة عندنا stateless — طلب واحد، رد واحد)
  - النص اللي المستخدم بيترجمه ممكن يكون إيه؟ (رسالة شغل، مستند، حاجة شخصية)
  - لو حد سألك "بياناتي بتروح فين؟" — تقول إيه؟
- [ ] لو فيه flag، طبّق قرارك.

> **مش هقولك الإجابة.** بس لاحظ إن ده أول قرار في المشروع فيه بُعد **خصوصية** مش أداء أو تكلفة، وإن الافتراضي مش بالضرورة اللي يناسبك.

---

## خارج الـ scope

- **Streaming / SSE** — أسماء الـ events اتغيرت هي كمان. تاسك لوحدها في M6.
- **`usage` / عدّ الـ tokens** — دي بتاعة العدّاد والاشتراكات، مش دلوقتي.
- **Function calling / tools** — إحنا مش عاملين agent.
- **`interactions.get`** واسترجاع محادثة قديمة.
- **ديون TASK-006 الخمسة المؤجلة** — قرارك سارٍ، مترجعش لها هنا.

---

## 🧪 أسئلة تجاوب عليها بالتجربة

**س1:** بعد أول ترجمة ناجحة، اطبع **الـ JSON الخام كامل** (log أو breakpoint) وحطه في التسليم. منه جاوب:
- الـ `model` رجع إيه بالظبط؟
- الـ `steps` فيها كام عنصر وأنواعهم إيه؟ (لو أكتر من واحد — ده بالظبط سبب البند اللي بيقول "اختار بالنوع")

**س2:** ابعت نص فاضي أو نص غريب جداً وشوف الـ `status` بيرجّع إيه. ده أرخص طريقة تشوف حالة غير `completed` بعينك.

⚠️ الـ free tier 20/يوم. التاسك دي محتاجة **عدة** طلبات ناجحة. لو لسه على الـ free tier، حدد الـ budget قبل ما تبدأ الجزء ب.

---

## مفاتيح تدور بيها

- `Interactions API reference` — Resource: Interaction
- `Gemini API versions explained` — عشان الـ path الصح
- ⚠️ `Api-Revision: 2026-05-20` — كان header إجباري قبل 8 يونيو 2026. الـ legacy schema اتحذف من ساعتها، فالأغلب مش محتاجه — **بس اتأكد**، لأن لو محتاجه ومش موجود هتاخد أخطاء شكلها مالوش معنى.
- `System.Text.Json polymorphic deserialization` — للـ `steps` مختلطة الأنواع. **بس اقرا الأول قبل ما تستعملها**: هل محتاج polymorphism حقيقي ولا `type` string وفلترة عادية تكفي؟ الحل الأبسط اللي بيشتغل هو الصح.
- `JsonNamingPolicy.SnakeCaseLower` — لو لمست حقول الـ `usage`

📎 [migrate-to-interactions](https://ai.google.dev/gemini-api/docs/migrate-to-interactions) · [interactions-overview](https://ai.google.dev/gemini-api/docs/interactions-overview) · [API reference](https://ai.google.dev/api/interactions-api) · [breaking changes May 2026](https://ai.google.dev/gemini-api/docs/interactions-breaking-changes-may-2026)

---

## Definition of Done

| # | الطلب | المتوقع |
|---|---|---|
| 1 | ترجمة عادية (`en` → `ar`) | **200** بنص مترجم حقيقي |
| 2 | حالة `status` مش `completed` | **مش** 200 — الـ status code المناسب من جدولك |
| 3 | كل اختبارات TASK-006 | لسه بترجّع 400 زي ما هي (مفيش regression) |

بلس: `dotnet build --no-incremental` → 0 warnings · `dotnet format --verify-no-changes` نضيف · **صفر** كود متعلّق عليه comment

---

## 📋 شكل التسليم

```
الجزء أ:  [ ] request DTO  [ ] الـ DTOs القديمة اتمسحت  [ ] الـ URL
الجزء ب:  [ ] response DTO  [ ] استخراج النص بالنوع مش بالموضع  [ ] حالة مفيش نص
الجزء ج:  [ ] جدول الـ status كامل بالأسباب  [ ] الحالات التلاتة الجديدة  [ ] SAFETY
الجزء د:  [ ] الـ model رجع ______  → قرار #18: ______
القرار:   [ ] التخزين — القرار ______ والسبب ______
الأسئلة:  س1 = (الصق الـ JSON)   س2 = ______
التحقق:   [ ] التلات حالات في الـ DoD  [ ] 0 warnings  [ ] format  [ ] مفيش comments
```


---

## 📜 السجل (اتنقل من progress.md في 2026-09-23 — النص زي ما هو)

| TASK-007 | هجرة الـ AI proxy لـ Gemini Interactions API + إعادة اشتقاق تصنيف الفشل | AI proxy | ✅ Done (Approved r2) | **التاسك اللي هو جابها.** الهيكل كان صح من r1 كله — DTOs، status enum، `ExtractText` بالنوع مش بالموضع، جدول الـ outcome. الـ blockers التلاتة في r1: (1) `Unkown` **و** `Unknown` في نفس الـ enum → التحذير كود ميت بـ 0 warnings، (2) الـ RAW dump اتساب فبيسجّل نص المستخدم كامل وبيلغي قرار `store: false` اللي هو نفسه أخده، (3) الـ 422 معلّق عليها — **دي طلعت مقصودة منه واتسحبت من التصنيف**. اتصلحوا في r2 وترجمة حقيقية عدّت |

- **اللي قبلها:** ✅ TASK-007 اتقفلت — Approved في r2 (2026-08-25). الهجرة شغالة end-to-end (ترجمة حقيقية رجعت من الـ Interactions API)، 0 warnings، format نضيف، ومفيش `Unkown` في أي ملف. الكوميت `fa6c556`.

### ⬜ مفتوح بعد TASK-007

1. ✅ ~~**قرار #18 معلّق**~~ — **اتقفل 2026-08-27 بدليل من الداتابيز بتاعته.** الرد جه بالـ alias `gemini-flash-latest` زي ما بعتناه بالظبط → القرار **اتقلب**، و**تثبيت اسم الموديل اتنقل من M8 لأقرب فرصة**. التفاصيل في جدول القرارات.
2. **🔴 الـ billing عايق فعلي** — اللوج لسه بيقول `free_tier, limit: 20`. اتأكد عملياً في الجلسة دي: الـ quota خلصت **مرتين** أثناء المراجعة لوحدها. أي تاسك جاية بتلمس الـ AI هتقف على ده. الترقية لـ Tier 1 = ربط billing account وبتشتغل فوراً، وسقفها $250.
3. **⬜ دَين الـ 422 (الحجب)** — مالهاش مصدر موثّق في الـ Interactions API. الـ `errors[]` بتتسجّل خام لحد ما نشوف حالة حقيقية.
4. **⬜ شكل `errors[]`** — مجهول لحد أول فشل.
5. **⬜ س1/س2 والسؤالين** — مجاوبش عليهم (الجلسة كانت طويلة جداً). **مايتحسبوش تخطّي** — الشغل نفسه اتحقق منه بالتشغيل.

**✅ اتحل ضمنياً بمجرد نجاح الرد:** `model` **string** مش object (وإلا كان `JsonException` → 502)، و`steps` فيها فعلاً `model_output` بـ `content[]` نص (وإلا `ExtractText` كانت رجّعت null).

### 🆕 اكتشاف كبير منه (2026-08-25) — Gemini Interactions API

راح قرا توثيق Google من نفسه ولقى إن **`generateContent` مبقتش المستحسنة**: الـ **Interactions API** بقت GA و Google بتقول بالنص إنها الـ recommended standard primitive لأي مشروع جديد. **المعلومة دي كانت غايبة عني** — اتأكدت منها وهو صح.

| القديم | الجديد |
|---|---|
| `POST v1beta/models/{model}:generateContent` بـ `{contents:[...]}` | `POST v1beta/interactions` بـ `{model, input}` |
| `candidates[0].content.parts[]` | `steps[].content[]` (جوه step نوعه `model_output`) |
| `finishReason` | `status` على الـ step |
| `modelVersion` | **مش موجود** |
| `usageMetadata.promptTokenCount` | `usage.prompt_tokens` (snake_case) |

⚠️ **breaking changes مايو 2026:** الـ `outputs` array اتشال لصالح `steps`، والـ legacy schema اتحذف 8 يونيو 2026 — يعني أي مثال قديم على النت بايظ.
📎 [migrate-to-interactions](https://ai.google.dev/gemini-api/docs/migrate-to-interactions) · [interactions-overview](https://ai.google.dev/gemini-api/docs/interactions-overview) · [breaking-changes-may-2026](https://ai.google.dev/gemini-api/docs/interactions-breaking-changes-may-2026)

**اتفصلت عن TASK-006 بقرار مني** (كانت هتخلي التاسكين غير قابلين للمراجعة) → بقت **TASK-007**. هو رجّع الملف نضيف من غير جدال.

**🔴 قرار #18 اتكسر بسببها:** مفيش `modelVersion` في الرد الجديد، يعني حقل `model` في `TranslationResponse` مالوش مصدر. القرار لازم **يتفتح تاني في TASK-007**، ولو الإجابة بقت `_options.Model` يبقى **تثبيت اسم الموديل** (المؤجل لـ M8) بقى ألزم مش أقل.
- **الوقت المتاح أسبوعياً:** ⏳ في انتظار الرد

- 🟢 **~~تكرار رابع لعادة الـ comment~~ — التصنيف ده اتسحب (TASK-007 r1).** أنا صنّفت الـ 422 المعلّق عليها كتكرار رابع لعادة "التعليق بدل الحذف". **وهو صحّح: التعليق كان مقصود** — عايز فاصل بصري قدامه يفكّره إن لازم يبقى فيه طريقة نعرف بيها إن النص اتحجب. النية دي **سليمة** ومختلفة تماماً عن التلات مرات اللي فاتت (تردد في الحذف). الاتنين شكلهم واحد في الـ diff، فالتصنيف من غير ما أسأله كان تسرّع مني.
  - **الملاحظة الباقية على المكانيكية مش على النية:** الكود المعلّق عليه بينادي `GeminiFinishReason.Safety` و`notCompleted.FinishReason` — **الاتنين اتمسحوا في نفس الـ commit**. يعني مش ممكن يتشال منه التعليق ويشتغل؛ هو **أحفورة** مش كود مؤجل. والتذكير اللي هو عايزه **موجود أصلاً** في التعليق اللي تحته مباشرة.
  - **الدرس للـ mentor:** لما شكل الـ diff يحتمل نيّتين، **اسأل قبل ما تسجّل نمط**. تسجيل نمط غلط في المفكرة أغلى من سؤال.

- **🔺🔺 راح لتوثيق المزوّد من نفسه ولقى حاجة الـ mentor مكنش يعرفها (2026-08-25).** اكتشف إن `generateContent` مبقتش المستحسنة وإن الـ Interactions API بقت GA — من غير ما حد يوجهه، وغيّر الكود على أساسها. **الاكتشاف صح واتأكد.** ده أعلى مستوى وصله لحد دلوقتي: مش بيقرا الـ ticket بعين ناقدة بس، بقى بيتحقق من **الافتراضات اللي الـ ticket نفسه مبني عليها**.
  - **اللي كان ناقص:** غيّر الـ request وساب الـ response — والنتيجة كانت هتبقى **502 على كل ترجمة ناجحة في صمت**. الاكتشاف ممتاز، الهجرة نصها. الدرس: **تغيير الـ API مش تغيير endpoint — هو تغيير الـ contract بالكامل، الاتجاهين.**
  - **واستجاب للفصل من غير جدال** لما اتقاله إن ده TASK-007 مش TASK-006.

