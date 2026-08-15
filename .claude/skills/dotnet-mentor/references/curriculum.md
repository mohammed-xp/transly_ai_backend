# Curriculum — from C# to a deployed backend

The running project is a **thin vertical slice of a backend for a Flutter app he already owns** (a delivery/logistics app is the likely default). Every milestone ships something the Flutter app could actually call.

Milestones are a map, not a contract. Skip what he already knows, expand where he struggles, reorder when the project demands it. Adapt task counts to his pace — the numbers below are a guide.

Rule of thumb for ordering: **something callable from Postman as early as possible.** Do not spend three weeks on C# syntax before he ever sees a 200 response.

---

## M0 — Intake & setup

Pick the app, scope the slice, extract the API contract from his Flutter models and Dio calls, agree the stack.

Tasks: install the SDK and confirm `dotnet --version`; create the solution and project; commit; get "Hello World" answering on `GET /health` from Postman.

**Unlocks:** a running server on day one. That matters psychologically far more than it sounds.

---

## M1 — C# for a Dart developer

Do **not** teach C# from zero. He knows programming; he needs the deltas. Cover only what differs from Dart, applied to the real domain models of the slice — no `Animal`/`Dog` toy exercises.

Focus:
- Types and records: `record` vs `class` vs `struct`, init-only properties, `required`, immutability (his Freezed instinct maps here).
- Nullable reference types — `<Nullable>enable</Nullable>` is on by default and the compiler is stricter than Dart's null safety in some places, looser in others.
- `Task` vs `Future`, `async`/`await` differences, `Task.WhenAll`, `CancellationToken`, and why `.Result`/`.Wait()` is forbidden.
- LINQ — the single biggest new skill. Method syntax, deferred execution, `IEnumerable<T>` vs `IQueryable<T>`.
- Interfaces, generics, extension methods, `IDisposable`/`using`.
- Naming and file conventions (see `flutter-to-dotnet.md`).

**Unlocks:** he can read any .NET codebase without guessing.

---

## M2 — First real endpoints (ASP.NET Core)

Focus:
- Minimal APIs vs Controllers — pick one *with an explicit decision block*. Controllers are the safer teaching default for a growing API; minimal APIs are excellent for small services. Justify whichever you pick.
- The built-in DI container (his `get_it` maps directly) — `AddScoped` / `AddSingleton` / `AddTransient` and the lifetime bugs each causes.
- `Program.cs`, the middleware pipeline, and why order matters.
- Configuration: `appsettings.json`, environments, user secrets — and never committing secrets.
- Routing, model binding, `[FromBody]` / `[FromQuery]` / `[FromRoute]`.
- **DTOs vs domain models** — never return an EF entity to the client. This is a decision block, and his Flutter response-model habit makes it land easily.
- Correct status codes and `ProblemDetails`.

**Unlocks:** the Flutter app can talk to it with fake in-memory data.

---

## M3 — Data layer (EF Core)

Focus:
- `DbContext`, `DbSet<T>`, connection strings, registering the context.
- Code-first migrations: `dotnet ef migrations add`, `dotnet ef database update`, and reading the generated migration before applying it.
- Relationships and navigation properties; `Include` and when it is needed.
- Change tracking; `AsNoTracking()` on reads.
- N+1 queries — show him the generated SQL. Nothing teaches this faster than watching the log.
- **The repository pattern decision.** He will want one. `DbContext` already is Unit of Work + Repository. Make him argue for it; usually the answer is no.

**Unlocks:** real persisted data, real migrations, a real database.

---

## M4 — Validation, errors, logging

Focus:
- DataAnnotations vs FluentValidation — decision block.
- Global exception handling middleware, `IExceptionHandler`, consistent error contracts.
- Structured logging with Serilog; log levels; correlation IDs across requests.
- Making the error shape match what his Flutter `Dio` interceptors and failure-mapping already expect — this is a genuinely nice moment where his client experience pays off.

**Unlocks:** an API that fails predictably, which is what makes a client app maintainable.

---

## M5 — Authentication & authorization

Focus:
- JWT: issuing, validating, claims, expiry, refresh tokens.
- ASP.NET Core Identity vs a hand-rolled user table — decision block.
- `[Authorize]`, roles vs policies.
- Password hashing; never storing plaintext.
- Reconciling with Firebase Auth if his app uses it — validating Firebase ID tokens server-side is a legitimate design and he has already built dual-signup logic on the client, so this connects directly.

**Unlocks:** a real login the Flutter app can use.

---

## M6 — Production concerns

Focus: pagination and filtering (and why `GetAll()` is a future outage), caching, file/image upload, background jobs (`IHostedService` / Hangfire) for things like status sweeps, sending FCM push from the server — he already handles the client half — rate limiting, CORS, health checks, Swagger/OpenAPI.

**Unlocks:** the API stops being a demo.

---

## M7 — Testing

Focus: xUnit basics and how they differ from Dart's `test`; unit tests for services; mocking with NSubstitute or Moq; integration tests with `WebApplicationFactory`; Testcontainers for a real database in tests; what is worth testing and what is not.

Point out where testing is genuinely easier than in Flutter — no widget tree, no pumping.

**Unlocks:** he can refactor without fear.

---

## M8 — Ship it

Focus: `Dockerfile` for the API, `docker compose` with the database, GitHub Actions CI (build + test on PR) — he already runs Fastlane + Actions for Play Store, so extend that muscle — deploy to Azure App Service or Container Apps, environment configuration and secrets in the cloud, then **point the Flutter app's Dio `baseUrl` at the deployed API and run it on a real device.**

**Unlocks:** the whole reason for the exercise. This is the moment the portfolio piece becomes real, and it is worth a proper retro.

---

## After M8

Options depending on where he wants to go: Azure services in depth (Blob Storage, Service Bus, Key Vault), SignalR for real-time driver tracking, performance profiling and caching strategy, or the React front-end that closes the full-stack loop he is aiming for. Ask him, do not decide for him.