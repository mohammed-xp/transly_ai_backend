# Review rubric

Read before every review. In Claude Code, always build, format-check, and test before commenting — an opinion on code you have not compiled is a guess.

```bash
dotnet build --no-incremental
dotnet format --verify-no-changes
dotnet test
git diff --stat && git diff
```

`--no-incremental` matters: an incremental "Build succeeded" can hide warnings that are still standing. If his app is running and locks `bin/` (MSB3027), build to a scratch output instead of killing his process: `dotnet build -p:BaseOutputPath=<scratchpad path>/`.

Before calling any earlier item "still open" — a debt, a should-fix, a missing test — confirm it in the code. `mentor/progress.md` describes the state at the last review, not the repo.

## The break check

A green test suite proves nothing until you have seen it go red for the right reason. For every guarantee the ticket claims:

1. Break the production line that provides it — remove the `[Authorize]`, reorder the two writes, flip the comparison, drop the filter.
2. Run the tests. Expect red, and read **which** tests fail and the failure message.
3. Restore, and confirm `git diff` is byte-identical to his submission.

- A test that stays green on a break → **finding**. Usually the assertion checks an outcome that has more than one cause (a `401` status) instead of the mechanism's signature (the `WWW-Authenticate: Bearer` challenge).
- If the ticket asked for tests that cannot catch that break at all, the gap is on the ticket's design — say so, and do not count it against him.
- The same failure message before and after one of his fixes means the fix did not touch the cause.

Report the break-check results as a small table: the line broken, the result, the failing test.

## Severity calls

- **🔴 Blocker** — breaks correctness, security, or teaches a habit that will hurt at scale (deadlocks, N+1 on a hot path, secrets in source, exposing entities, missing auth, a migration that does not apply). Max **3 per review**.
- **🟡 Should fix** — works but is not idiomatic; the gap between "compiles" and "a .NET developer wrote this".
- **🔵 Nit** — naming, formatting, a cleaner LINQ shape. One line each. Never more than a handful.

If a review has zero blockers, approve it. Manufacturing a blocker to look rigorous is as bad as rubber-stamping.

A failing `dotnet format`, an unaddressed note from the previous round, or a missed ticket criterion is a finding at its own severity. Never return a review unread because of it.

## Correctness and async

- `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` anywhere in request code → **blocker** (deadlock risk, thread-pool starvation).
- `async void` outside an event handler → blocker.
- Async all the way down; no sync wrapper over async.
- `CancellationToken` accepted and passed through to EF Core and HTTP calls.
- No `Thread.Sleep` in async code (`Task.Delay`).
- Defensive code that can never run: a guard on a path that throws before it, a validation method on a type that never implements the interface the framework calls, a comparison against a misspelled enum member. All compile with zero warnings. Make the case happen and watch the guard fire — or it is not a guard.

## EF Core

- **N+1**: a query inside a loop, or a missing `Include`. Show him the generated SQL rather than asserting it.
- `AsNoTracking()` on read-only queries.
- `IQueryable` accidentally materialized early (`.ToList()` then `.Where()`) — the filter now runs in memory over the whole table.
- Filtering/paging applied in the database, not after fetching everything.
- Migrations generated, never hand-edited: if the migration is wrong, fix the model and regenerate. Committed to the repo.
- Migrations **applied** against the real provider (the integration tests or `dotnet ef database update`), not only read. EF Core generates provider-agnostic operations the provider may reject — MySQL refuses to drop an index a foreign key still needs.
- `DbContext` never registered as a singleton, never captured by one (captive dependency), never shared across threads.
- Raw SQL parameterized. String-concatenated SQL → blocker.
- Tests run against the real provider, not the InMemory provider — it has no unique indexes, no foreign keys, no cascades, so a test can pass while proving nothing about the real table.

## API design

- EF entities returned directly from an endpoint → blocker. DTOs always.
- Correct status codes: 201 + `Location` on create, 204 on delete, 404 for missing, 400 for bad input, 401 vs 403 used correctly, 429 for our own limits vs 503 for an upstream provider's.
- Consistent error contract (`ProblemDetails`), not ad-hoc anonymous objects.
- Input validated at the boundary.
- Route naming: plural nouns, no verbs (`/api/orders/{id}/status`, not `/api/getOrderById`).
- Nothing sensitive in query strings.
- Designed for any mobile client — never shaped after one client's code.

## Security

- Secrets in `appsettings.json` committed to git → blocker. User secrets locally, environment variables or Key Vault in production.
- Passwords hashed with a real algorithm, never plaintext, never MD5/SHA1.
- Endpoints authorized by default; anonymous access is the explicit exception.
- Authorization checks on the **resource**, not just the endpoint — can driver A read driver B's orders by changing the id?
- CORS not `AllowAnyOrigin` in production.

## Structure and style

- Over-abstraction: an interface with one implementation and no test seam; a repository wrapping `DbContext` with no added behavior; four projects for a five-endpoint API. Call it out with the cost, not just the label.
- Fat controllers with business logic inline — acceptable early, worth naming once it grows.
- Naming conventions (see `flutter-to-dotnet.md`): `PascalCase` members, `_camelCase` private fields, `I`-prefixed interfaces, `Async` suffix.
- One public type per file, and the type name matches the file name.
- Nullable reference types enabled and warnings not suppressed with `!` scattered everywhere. A warning silenced by weakening a type (`string?`, `= null!`) instead of fixing the design is a finding.
- Broad `catch (Exception)` that swallows and continues.
- Dead code, commented-out blocks, leftover `Console.WriteLine` debugging.

## Teaching notes while reviewing

- **Show, do not just assert.** For an N+1, the SQL log is the argument. For a deadlock, the mechanism is the argument. For a weak test, the break-check result is the argument.
- **Give the cost, not the label.** "This is not idiomatic" teaches nothing; "this materializes the whole orders table into memory before filtering, so it will be fine on 50 rows and fall over on 50,000" teaches everything.
- **Distinguish rule from taste.** He should know which findings are hard rules and which are one team's convention.
- **Protect decisions with mechanisms, not comments.** Never ask for an explanatory comment. If a line order or a status code must not change, the finding asks for a test, a policy, or a type that enforces it. If none fits, log the debt with its cost.
- **Judge «ساعدني» work on what he did with it.** When the code was handed over, the review is about what he ran, verified, and changed — not about authorship.
- **Track patterns, not incidents.** Log recurring weaknesses in `mentor/progress.md`. The third time the same mistake appears, stop fixing it in review and assign a ticket that targets it directly — when he next asks for one. When the diff allows two readings of his intent, ask before you log a pattern.
- **Do not rewrite his code in the review.** Point at the line, name the problem, let him fix it. If he needs more, use the hint ladder — or he can say «ساعدني».
