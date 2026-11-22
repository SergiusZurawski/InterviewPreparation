# 04 · C# / .NET Fundamentals (refresh for technical questions)

## 1. async / await
- `async` method → compiler-generated **state machine**. `await` on an incomplete task registers a continuation and **returns the thread** to the pool. No thread is blocked during I/O.
- **CPU-bound vs I/O-bound:** `await` for I/O; `Task.Run` only to offload CPU work (and not in ASP.NET request paths just to "make it async").
- `ConfigureAwait(false)`: don't capture the sync context. Relevant in **libraries** and UI apps. ASP.NET Core has no SynchronizationContext, so it mostly doesn't matter there.
- `Task.WhenAll`: concurrent awaits. `await` rethrows only the **first** exception; the others are in `task.Exception.InnerExceptions`.
- `ValueTask`: for hot paths that often complete synchronously. **Await it only once.**
- `IAsyncEnumerable<T>` + `await foreach`: streaming. `[EnumeratorCancellation]` for the token.
- Cancellation: cooperative. `ct.ThrowIfCancellationRequested()`, pass `ct` down. `OperationCanceledException` is not an error, so don't log it as one.
- Sync-over-async (`.Result`) → thread-pool starvation; async-over-sync (`Task.Run(syncIO)`) → wasted thread.

## 2. Memory & types
- Value types (struct) are copied; reference types (class) share a reference. Boxing happens when a value type is stored as `object`/interface.
- GC: generations 0/1/2. The **LOH** holds objects ≥ 85 KB, collected with gen 2. Strings are immutable → use `StringBuilder` in loops.
- `IDisposable` releases unmanaged resources/connections deterministically: `using var x = …;` / `await using` for `IAsyncDisposable`. Not disposing → connection pool exhaustion.
- `Span<T>` / `Memory<T>`: slices without allocation (know what they are, no need to go deep).

## 3. Records, equality, immutability
- `record` (class) / `record struct`: **value equality**, `with` expressions, a deconstructor, and a nice `ToString`. Good for DTOs.
- Overriding `Equals` without `GetHashCode` → broken `Dictionary`/`HashSet`.
- `init` setters, `required` members (C# 11).
- Records as EF entities → caution (value equality vs tracking identity).

## 4. Nullability
- Nullable reference types (`string?`) are compile-time analysis only.
- **`!` (null-forgiving) is a review smell.** Often hides `FirstOrDefault()` returning null.
- `??`, `?.`, `??=`, `is null` / `is not null` (pattern matching, ignores overloaded `==`).

## 5. LINQ
- **Deferred execution**: `IEnumerable` queries rerun on every enumeration → multiple enumeration bugs (performance, or different results if the source changes). Materialise once.

| Method | Throws when | Returns when nothing matches |
|---|---|---|
| `First` | empty | — |
| `FirstOrDefault` | — | null/default |
| `Single` | empty **or** more than one | — |
| `SingleOrDefault` | more than one | null/default |

- `Any()` vs `Count() > 0`; `List.Contains` in a loop is O(n²) → `HashSet`.
- `GroupBy`, `ToDictionary` (throws on duplicate keys!), `ToLookup`.

## 6. Collections & complexity

| Type | Lookup | Notes |
|---|---|---|
| `List<T>` | O(n) search, O(1) index | Not thread-safe |
| `Dictionary<K,V>` | O(1) avg | Not thread-safe for writes |
| `HashSet<T>` | O(1) contains | |
| `ConcurrentDictionary` | O(1) | `GetOrAdd` factory may run more than once |
| `ImmutableList` / `FrozenDictionary` (.NET 8) | | Read-heavy shared data |

Expose `IReadOnlyList<T>` / `IReadOnlyCollection<T>` from APIs instead of a mutable `List<T>`.

## 7. Thread safety
- `lock` (sync only; you **can't `await` inside `lock`**) → use `SemaphoreSlim(1,1)` with `WaitAsync` for async critical sections.
- `Interlocked.Increment` for counters.
- `static` mutable fields in a web app are shared across all requests.
- C# 13 / .NET 9: `System.Threading.Lock` type.

## 8. Exceptions
- `throw;` preserves the stack trace; `throw ex;` resets it.
- Catch specific exceptions; `catch (Exception)` only at boundaries (middleware, job loop).
- Exception filters: `catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)`.
- Don't use exceptions for control flow in hot paths; consider a Result pattern for expected failures.
- Wrapping: `throw new DomainException("…", ex)` keeps the inner exception.

## 9. Strings, numbers, dates
- Comparisons: `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)`, not `a.ToLower() == b.ToLower()` (allocations, culture — the Turkish "I" problem).
- `decimal` for money; `double` has binary rounding errors. Watch integer division (`5 / 2 == 2`).
- `DateTime.Now` → local server time (wrong on servers). Use `DateTime.UtcNow` / `DateTimeOffset` / `TimeProvider`.
- Date ranges: use half-open `[from, to)` to avoid missing or duplicate boundary records.
- `int` overflow is unchecked by default (`checked` context).

## 10. Modern C# features (they may show up in the diff)

| Feature | Version | Watch for |
|---|---|---|
| Records, init, pattern matching, switch expressions | C# 9 | |
| Global usings, file-scoped namespaces, minimal hosting | C# 10 / .NET 6 | |
| `required` members, raw string literals, list patterns | C# 11 | |
| **Primary constructors** | C# 12 | Parameters are **captured mutable variables, not readonly fields**. AI code may reassign them or double-capture (field + parameter). |
| Collection expressions `[1, 2, 3]` | C# 12 | |
| `params` collections, `Lock` type, `field` keyword preview | C# 13 / .NET 9 | |
| Extension members, `field` keyword, null-conditional assignment `a?.B = x` | C# 14 / **.NET 10 LTS** (Nov 2025) | |

## 11. Runtime & platform
- .NET (Core) vs .NET Framework: cross-platform, Kestrel, built-in DI/config/logging, side-by-side.
- **LTS** releases are even-numbered: .NET 8 (support ends Nov 2026) and .NET 10 (current LTS). Expect a TRG codebase on 8 or 10.
- JIT, tiered compilation; Native AOT (startup and size; reflection limits).
- GC modes: Server GC is the default for ASP.NET Core.

## 12. SOLID / design, briefly
- SRP and DI are the most visible in reviews. Prefer composition; don't add interfaces with a single implementation unless they're needed for testing or as a boundary.
- YAGNI is valuable when reviewing AI code that over-abstracts.
