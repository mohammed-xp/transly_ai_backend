# Review rubric

Read before every review. In Claude Code, always build and test before commenting — an opinion on code you have not compiled is a guess.

```bash
dotnet build
dotnet test
git diff main --stat && git diff main
```

## Severity calls

- **🔴 Blocker** — breaks correctness, security, or teaches a habit that will hurt at scale (deadlocks, N+1 on a hot path, secrets in source, exposing entities, missing auth). Max **3 per review**.
- **🟡 Should fix** — works but is not idiomatic; the gap between "compiles" and "a .NET developer wrote this".
- **🔵 Nit** — naming, formatting, a cleaner LINQ shape. One line each. Never more than a handful.

If a review has zero blockers, approve it. Manufacturing a blocker to look rigorous is as bad as rubber-stamping.

## Correctness and async

- `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` anywhere in request code → **blocker** (deadlock risk, thread-pool starvation).
- `async void` outside an event handler → blocker.
- Async all the way down; no sync wrapper over async.
- `CancellationToken` accepted and passed through to EF Core and HTTP calls.
- No `Thread.Sleep` in async code (`Task.Delay`).

## EF Core

- **N+1**: a query inside a loop, or a missing `Include`. Show him the generated SQL rather than asserting it.
- `AsNoTracking()` on read-only queries.
- `IQueryable` accidentally materialized early (`.ToList()` then `.Where()`) — the filter now runs in memory over the whole table.
- Filtering/paging applied in the database, not after fetching everything.
- Migrations reviewed before applying, and committed to the repo.
- `DbContext` never registered as a singleton, never captured by one (captive dependency), never shared across threads.
- Raw SQL parameterized. String-concatenated SQL → blocker.

## API design

- EF entities returned directly from an endpoint → blocker. DTOs always.
- Correct status codes: 201 + `Location` on create, 204 on delete, 404 for missing, 400 for bad input, 401 vs 403 used correctly.
- Consistent error contract (`ProblemDetails`), not ad-hoc anonymous objects.
- Input validated at the boundary.
- Route naming: plural nouns, no verbs (`/api/orders/{id}/status`, not `/api/getOrderById`).
- Nothing sensitive in query strings.

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
- One public type per file.
- Nullable reference types enabled and warnings not suppressed with `!` scattered everywhere.
- Broad `catch (Exception)` that swallows and continues.
- Dead code, commented-out blocks, leftover `Console.WriteLine` debugging.

## Teaching notes while reviewing

- **Show, do not just assert.** For an N+1, the SQL log is the argument. For a deadlock, the mechanism is the argument.
- **Give the cost, not the label.** "This is not idiomatic" teaches nothing; "this materializes the whole orders table into memory before filtering, so it will be fine on 50 rows and fall over on 50,000" teaches everything.
- **Distinguish rule from taste.** He should know which findings are hard rules and which are one team's convention.
- **Track patterns, not incidents.** Log recurring weaknesses in `mentor/progress.md`. The third time the same mistake appears, stop fixing it in review and assign a ticket that targets it directly.
- **Do not rewrite his code in the review.** Point at the line, name the problem, let him fix it. If he needs more, use the hint ladder.