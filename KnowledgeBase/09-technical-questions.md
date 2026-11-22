# 09 · Technical Question Bank (quick-fire revision)

> Answer in about 30–60 seconds: definition → when it matters → pitfall/trade-off → example from your experience if possible.

## C# / .NET
1. **What does `async/await` actually do?** It compiles into a state machine. At `await` on incomplete I/O the thread returns to the pool and a continuation resumes the method later. Nothing is blocked during I/O, which gives scalability rather than speed.
2. **Why is `.Result` bad in ASP.NET Core?** It blocks a thread-pool thread, which leads to thread-pool starvation under load. (Classic ASP.NET and UI apps can also deadlock.)
3. **`Task` vs `ValueTask`?** `ValueTask` avoids an allocation when the result is often synchronous. Await it only once; the default choice is `Task`.
4. **`IEnumerable` vs `IQueryable`?** `IEnumerable` is in-memory LINQ to Objects; `IQueryable` is an expression tree translated by a provider such as EF into SQL. Switching to `IEnumerable` early pulls all the data into memory.
5. **Deferred execution and multiple enumeration?** A LINQ query runs on every enumeration. Materialise it once when you'll reuse it.
6. **`record` vs `class` vs `struct`?** Record: value equality, `with`, immutability-friendly, good for DTOs. Struct: value type, copied, for small data.
7. **`throw` vs `throw ex`?** `throw` preserves the stack trace.
8. **`IDisposable` and `using`?** Deterministic cleanup of unmanaged resources such as connections and streams. `await using` for async disposal.
9. **Thread-safe counter / async lock?** `Interlocked`; `SemaphoreSlim.WaitAsync`. You can't `await` inside a `lock`.
10. **What's new in recent C#?** Primary constructors (parameters are captured mutable variables), collection expressions, `required`, the `field` keyword and extension members in C# 14 / .NET 10.
11. **Garbage collection basics?** Generational (0/1/2), with a large object heap for objects of 85 KB or more. Server GC in ASP.NET Core. Reduce allocations on hot paths.
12. **`decimal` vs `double`?** `decimal` for money (base-10 exact); `double` for scientific values.

## ASP.NET Core
13. **DI lifetimes and the captive dependency problem?** See [03](03-aspnet-core-pitfalls.md#1-dependency-injection-lifetimes).
14. **Middleware pipeline: what is it and does order matter?** A chain of request delegates; order is execution order. Authentication goes before Authorization, and the exception handler goes first.
15. **How do you handle errors globally?** `UseExceptionHandler` plus `IExceptionHandler` plus ProblemDetails. Don't leak internals, and map domain exceptions to status codes.
16. **`IOptions` vs `IOptionsSnapshot` vs `IOptionsMonitor`?** Singleton with no reload / scoped per request / singleton with change notifications.
17. **How do you call external HTTP APIs correctly?** `IHttpClientFactory` and typed clients, timeouts, resilience (retry for idempotent calls, circuit breaker), cancellation.
18. **How do you run background work?** `BackgroundService` with a scope per iteration, a `Channel<T>` for in-process queues, a message broker for durability. Multiple replicas need a distributed lock or a queue.
19. **Authentication vs authorization; how do you protect a resource?** JWT/cookies identify the caller. Policies and requirements decide access, and resource-based authorization checks ownership or tenant.
20. **How do you version an API?** URL, header, or media type (`Asp.Versioning`). Make additive changes, deprecate gradually.
21. **Minimal APIs vs controllers?** Minimal: less ceremony, faster, endpoint filters. Controllers: conventions, filters, larger APIs. Both are fine.

## EF Core (prepare these hardest)
22. **What is the change tracker?** It snapshots loaded entities. `SaveChanges` compares them, generates INSERT/UPDATE/DELETE, and runs them in one transaction.
23. **What is N+1 and how do you fix it?** A query per item in a loop, or lazy loading. Fix with a projection, `Include`, or batch loading with `Contains`.
24. **`Include` vs projection?** Projection fetches only the needed columns and isn't tracked, so it's best for reads. Use `Include` when you need entities to modify.
25. **When do you use `AsNoTracking`?** Read-only queries. It saves memory and CPU.
26. **`AsSplitQuery`?** Avoids cartesian explosion with multiple collection Includes, at the cost of extra round-trips and consistency.
27. **How do you handle concurrency?** A rowversion concurrency token, catch `DbUpdateConcurrencyException`, return 409 or retry. Or use atomic `ExecuteUpdate`. Unique constraints for check-then-insert.
28. **Transactions in EF?** `SaveChanges` is atomic. Use `BeginTransaction` for several operations, and the execution strategy when retries are enabled.
29. **Raw SQL safely?** `FromSql` or `FromSqlInterpolated` (parameterised). Never `FromSqlRaw` with interpolation.
30. **DbContext lifetime and thread safety?** Scoped and not thread-safe. Use `IDbContextFactory` in singletons and background jobs, and never run parallel queries on one context.
31. **EF Core vs Dapper: when would you use each?** EF: productivity, change tracking, migrations, LINQ, ideal for CRUD and the domain model. Dapper: full SQL control and performance for complex reporting and bulk reads. Mixing them is fine; EF has `SqlQuery` too. (Your experience.)
32. **How do you test EF code?** Integration tests against real SQL Server (Testcontainers). Avoid the InMemory provider for queries.
33. **Migrations in CI/CD?** Generate idempotent scripts or migration bundles and apply them in the pipeline, not at app startup with many replicas. Review generated migrations for data loss.

## Testing & quality
34. **Unit vs integration tests: what's your strategy?** The test pyramid adapted to the risk. For APIs: many integration tests with `WebApplicationFactory` plus a real DB, and unit tests for the domain logic.
35. **How do you know a test is good?** It fails when the behaviour breaks (mutation mindset), is deterministic, tests behaviour not implementation, and its name explains the scenario.
36. **How do you debug a production performance issue?** Metrics → traces → narrow to a layer → SQL plans / `dotnet-counters` / dumps → fix plus a regression test plus monitoring.

## AI & engineering practice
37. **How do you use AI in your daily work?** See [07 §7](07-ai-assisted-review.md#7-your-ai-workflow-story-short-version-for-how-do-you-use-ai-day-to-day).
38. **How do you verify AI-generated code?** The same as a junior's PR, but more sceptically about APIs and edge cases: read it, compile it, test it, check the docs, and ask for evidence.
39. **What are the risks of AI-generated code?** Plausible but wrong logic, outdated or hallucinated APIs, security gaps, over-engineering, weak tests, licence or leakage concerns, and the reviewer becoming complacent.
40. **How should a team adapt code review for AI-written PRs?** Smaller PRs, author must understand and own every line, tests required, the AI-written parts flagged, a security checklist, and the reviewer able to take over the code (your code-review story).

## Full stack (they may ask; it's a full-stack role)
41. **Frontend experience?** Vue 3 + TypeScript (PIAD, Quadriss). Map the concepts if they use React or Angular: components, reactive state, composables ≈ hooks/services, routing, API client layer.
42. **SPA security?** Tokens in memory or HttpOnly cookies, XSS (framework escaping, avoid `v-html`/`innerHTML`), CORS, CSP.
43. **Performance on the frontend?** Pagination or virtual scrolling for big tables, debounced filters, caching API calls, lazy-loaded routes.
