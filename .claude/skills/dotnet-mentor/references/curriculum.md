# Curriculum — from C# to a deployed backend

The running project is a **thin vertical slice of a real backend for a mobile product** — for TranslyAI, a translation API. Every milestone ships something a mobile client could actually call. The API is client-agnostic: its contract comes from the use case, described in words, never from the Flutter code.

Milestones are a map, not a contract. Skip what he already knows, expand where he struggles, reorder when the project demands it. Tickets are sized by content, never by his time — the numbers below are a guide. He chooses the direction of each new ticket from the options you offer, and a milestone pulled forward for a reason goes back to this path once the reason is resolved.

Rule of thumb for ordering: **something callable from Postman as early as possible.** Do not spend three weeks on C# syntax before he ever sees a 200 response.

---

## M0 — Intake & setup

Pick the product, scope the slice, define the API contract from the use case in words, agree the stack.

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

**Unlocks:** a mobile client can call it, even with fake in-memory data.

---

## M3 — Data layer (EF Core)

Focus:
- `DbContext`, `DbSet<T>`, connection strings, registering the context.
- Picking the provider package for the target framework. For MySQL on EF Core 10 that is Oracle's `MySql.EntityFrameworkCore`: Pomelo is in every tutorial but stopped at EF Core 9. Check NuGet, not the tutorial.
- Code-first migrations: `dotnet ef migrations add`, `dotnet ef database update`, reading the generated migration before applying it, and applying it for real — generated operations are provider-agnostic and the provider can still reject them. A wrong migration is fixed in the model and regenerated, never hand-edited.
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
- An error contract any mobile client can map to its own failure types without guessing — designed from the API side. This is where his client experience pays off: he has written the `Dio` interceptors that suffer from inconsistent error shapes, so he knows what a good contract must guarantee.

**Unlocks:** an API that fails predictably, which is what makes a client app maintainable.

---

## M5 — Authentication & authorization

Focus:
- JWT: issuing, validating, claims, expiry, refresh tokens.
- ASP.NET Core Identity vs a hand-rolled user table — decision block.
- `[Authorize]`, roles vs policies.
- Password hashing; never storing plaintext.
- External identity providers (Firebase, Google sign-in) — validating their ID tokens server-side is a legitimate design, but only when the product needs it. Ask about the requirement; do not infer it from client code.

**Unlocks:** a real login any mobile client can use.

---

## M6 — Production concerns

Focus: pagination and filtering (and why `GetAll()` is a future outage), caching, file/image upload, background jobs (`IHostedService` / Hangfire) for things like status sweeps, sending FCM push from the server — he already handles the client half — rate limiting, CORS, health checks, Swagger/OpenAPI.

**Unlocks:** the API stops being a demo.

---

## M7 — Testing

Focus: xUnit basics and how they differ from Dart's `test`; unit tests for services; mocking with NSubstitute or Moq; integration tests with `WebApplicationFactory`; a real database in tests (Testcontainers, or a schema rebuilt from the migrations on every run) — never the InMemory provider, which has no constraints to test against; asserting on a mechanism rather than an outcome with several causes; breaking the guarded line to watch the test go red; what is worth testing and what is not.

Point out where testing is genuinely easier than in Flutter — no widget tree, no pumping.

**Unlocks:** he can refactor without fear.

---

## M8 — Ship it

Focus: `Dockerfile` for the API, `docker compose` with the database, GitHub Actions CI (build + test on PR) — he already runs Fastlane + Actions for Play Store, so extend that muscle. Integration tests that depend on a local database and user secrets need portable configuration first (a CI service container or Testcontainers). Then deploy to Azure App Service or Container Apps, set environment configuration and secrets in the cloud, and **run a real mobile client on a real device against the deployed URL.**

**Unlocks:** the whole reason for the exercise. This is the moment the portfolio piece becomes real, and it is worth a proper retro.

---

## After M8

Options depending on where he wants to go: Azure services in depth (Blob Storage, Service Bus, Key Vault), streaming responses (SSE or SignalR), performance profiling and caching strategy, or the React front-end that closes the full-stack loop he is aiming for. Ask him, do not decide for him.