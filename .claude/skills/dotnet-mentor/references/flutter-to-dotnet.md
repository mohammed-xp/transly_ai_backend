# Flutter/Dart → .NET bridge

Use this to explain new concepts by anchoring them to what he already owns. An accurate analogy saves twenty minutes of explanation. A sloppy one costs an hour of debugging — so always name where the analogy breaks.

## Language

| Dart | C# | Where it breaks |
|---|---|---|
| `Future<T>` | `Task<T>` | `Future` runs on creation; a `Task` from `async` also starts immediately, but a cold `Task` from `new Task()` does not. Never use `new Task()`. |
| `async`/`await` | `async`/`await` | Nearly identical in feel. But `.Result` / `.Wait()` can deadlock — in Dart there is no equivalent footgun. `async void` is forbidden except in event handlers. |
| `Stream<T>` | `IAsyncEnumerable<T>` / `IObservable<T>` | Not a drop-in. Streams in .NET are far less central than in Flutter — do not reach for Rx by reflex. |
| Null safety (`String?`) | Nullable reference types (`string?`) | .NET's is a **compiler warning system**, not a runtime guarantee. A `string` can still be null at runtime if it came from old code or JSON. |
| `late` | no equivalent | Use `required`, constructor injection, or `null!` (sparingly). |
| `final` field | `readonly` field | `const` in C# means compile-time constant — much narrower than Dart's `const`. |
| Freezed data class | `record` | `record` gives value equality, `with`-expressions (`copyWith`), and immutability built into the language. He will like this. |
| `List<T>.where().map()` | LINQ `.Where().Select()` | LINQ over `IQueryable` translates to **SQL**. That is the big one — the same expression can run in memory or in the database, with wildly different performance. |
| Extension methods | Extension methods | Same idea, same usefulness. |
| Mixins | Interfaces with default implementations | Weaker in C#. Prefer composition. |
| `dynamic` | `dynamic` | Same code smell in both. |

## Architecture and tooling

| Flutter world | .NET world | Notes |
|---|---|---|
| `get_it` | Built-in DI container in `Program.cs` | Direct match. `AddSingleton` / `AddScoped` / `AddTransient`. **Scoped = per HTTP request** — a concept with no Flutter equivalent, and the source of most beginner DI bugs. |
| `Dio` interceptors | Middleware pipeline | Same mental model: a chain wrapping each request. Order matters in both. |
| BLoC / Cubit | *(nothing)* | Do not look for it. A stateless request/response API has no state machine. If he reaches for MediatR to recreate the feeling, push back. |
| Clean Architecture layers | Same layering is possible | .NET culture starts simpler and splits when pain appears. Vertical Slice Architecture is the modern counter-current — worth showing him. |
| Repository over Dio | Repository over EF Core | Usually **not** needed. `DbContext` is already Unit of Work; `DbSet<T>` is already a repository. |
| `json_serializable` / build_runner | `System.Text.Json` | No codegen step. Reflection-based by default, source generators optional. He will enjoy losing build_runner. |
| `pubspec.yaml` | `.csproj` + NuGet | `dotnet add package`. |
| FVM | `global.json` | Pins the SDK version per repo. |
| `flutter analyze` | Compiler + analyzers + `.editorconfig` | The C# compiler catches far more before runtime than the Dart one. |
| Response models | DTOs | Same discipline he already applies client-side: never expose the internal shape. |
| Fastlane + GitHub Actions | Docker + GitHub Actions | The CI muscle transfers directly. |
| `flutter test` | `dotnet test` (xUnit) | No widget tree, no `pumpAndSettle`. Testing is genuinely simpler here. |

## Conventions he will get wrong at first

Coming from Dart, these are the reflexes to correct early and once:

- **Method and property names are `PascalCase`**, not `camelCase`. Local variables and parameters are `camelCase`.
- Private fields are `_camelCase`.
- Interfaces are prefixed `I` (`IOrderService`) — unusual to a Dart eye, universal in .NET.
- Async methods end in `Async` (`GetOrdersAsync`).
- **One public type per file, file named after the type.** No `snake_case.dart`-style filenames, no barrel files.
- Folders map to namespaces.
- `var` is idiomatic in C# when the type is obvious on the right-hand side — unlike Dart style guides, nobody objects.
- Braces on their own line is the dominant .NET style. Let the formatter handle it (`dotnet format`).

## Where his Flutter experience is an actual advantage

Say this out loud when it applies — it is true and it keeps him oriented:

- He already knows what a good API looks like **from the consumer side**. He has been hurt by inconsistent error shapes, missing pagination, and chatty endpoints. That makes him better at designing them than most junior backend developers.
- He has already implemented auth token refresh, 403 handling, and session invalidation on the client, so JWT lifetimes and refresh flows are half-familiar.
- He already ships CI/CD to a store, which is a harder deployment story than most web APIs.
- He already thinks in DI and layered architecture. The vocabulary transfers; only the syntax is new.