# 03 · ASP.NET Core Review Pitfalls

## 1. Dependency injection lifetimes

| Lifetime | Instance | Typical use |
|---|---|---|
| Transient | New every resolve | Lightweight stateless services |
| Scoped | One per HTTP request / scope | `DbContext`, unit of work, per-request user context |
| Singleton | One for app lifetime | Caches, config, `IHttpClientFactory`, stateless thread-safe services |

**Captive dependency:** a singleton depends on a scoped/transient service, so the scoped one lives forever (stale DbContext, shared across threads → corruption). In Development, `ValidateScopes`/`ValidateOnBuild` catch some of these at startup; in Production they're **off by default**.

**Singletons must be thread-safe.** `Dictionary<,>`/`List<>` fields mutated in a singleton → use `ConcurrentDictionary` / locks / immutable snapshots.

Other DI smells: service locator (`IServiceProvider.GetService` everywhere), `new`-ing dependencies inside classes, `BuildServiceProvider()` called inside `ConfigureServices` (creates a second container).

.NET 8: keyed services (`AddKeyedSingleton<IFoo, Foo>("a")`, `[FromKeyedServices("a")]`).

## 2. Async in a web app

| Smell | Why it's bad | Fix |
|---|---|---|
| `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` | Blocks a thread-pool thread → **thread-pool starvation** under load (ASP.NET Core has no SynchronizationContext, so deadlock is less likely than in classic ASP.NET, but starvation is real) | `await` all the way |
| `async void` | Exceptions crash the process; can't be awaited | `async Task` (only event handlers may be `async void`) |
| Fire-and-forget `_ = Task.Run(() => DoWork(db))` in a controller | Request scope is disposed → `ObjectDisposedException` on DbContext; exceptions lost; work lost on shutdown | Queue to a `BackgroundService` (`Channel<T>`), or a proper message queue |
| Using `HttpContext` on another thread | Not thread-safe; invalid after request ends | Copy the values you need first |
| `CancellationToken` missing / not forwarded | Abandoned requests keep consuming DB resources | Accept `CancellationToken ct` in the action (bound to `HttpContext.RequestAborted`) and pass it to every async call |
| `Task.WhenAll` over the same DbContext | Concurrent use → exception | Sequential, or separate contexts via factory |
| `Thread.Sleep` | Blocks a thread | `await Task.Delay(…, ct)` |

## 3. HttpClient

```csharp
// ❌ new per call → socket exhaustion (TIME_WAIT), no pooling
using var client = new HttpClient();

// ❌ static forever → ignores DNS changes (unless PooledConnectionLifetime set)
static readonly HttpClient Client = new();

// ✅ IHttpClientFactory, typed client
services.AddHttpClient<IPricingClient, PricingClient>(c => {
    c.BaseAddress = new Uri(opts.BaseUrl);
    c.Timeout = TimeSpan.FromSeconds(10);
}).AddStandardResilienceHandler();   // Microsoft.Extensions.Http.Resilience (.NET 8+): retry, timeout, circuit breaker
```
Also: check `response.IsSuccessStatusCode` / `EnsureSuccessStatusCode()`, dispose `HttpResponseMessage`, **don't retry non-idempotent POSTs blindly**, and never log full responses that contain PII or tokens.

## 4. Controllers / endpoints

- **DTOs, not entities**, in and out:
  - Entities as input → **overposting** (client sets `IsAdmin`, `TenantId`).
  - Entities as output → leaks internal fields, causes serialization cycles, and couples the API to the schema.
- `[ApiController]` → automatic 400 `ValidationProblemDetails` when the `ModelState` is invalid. Validation via DataAnnotations or FluentValidation. Check that **business rules** are also validated (e.g. `from <= to`, max range size).
- Correct status codes:

| Situation | Status |
|---|---|
| Created | `201 CreatedAtAction` |
| Not found **or not yours** | `404` (don't reveal existence) or `403` |
| Validation | `400` |
| Concurrency / duplicate | `409` |
| Unhandled | `500` with ProblemDetails, **no stack trace / exception message** |

- Global error handling: `app.UseExceptionHandler()` + `IExceptionHandler` (.NET 8) + `AddProblemDetails()`. A `try/catch` in every action is a smell, and so is `catch (Exception ex) { return BadRequest(ex.Message); }` (leaks internals and gives the wrong status).
- Unbounded list endpoints → paging + max page size.
- Async actions return `Task<ActionResult<T>>` / `Results<Ok<T>, NotFound>` (minimal APIs typed results).

## 5. Middleware order (common in Program.cs diffs)

```csharp
app.UseExceptionHandler();      // first, so it wraps everything
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();                  // after routing, before auth
app.UseAuthentication();        // who are you
app.UseAuthorization();         // what may you do (must come after authentication)
app.UseRateLimiter();           // .NET 7+
app.MapControllers();
```
Authorization before authentication = everyone is anonymous. Also look for `AllowAnyOrigin()` + `AllowCredentials()` (invalid / insecure), and Swagger exposed in production.

## 6. Configuration & options

- Options pattern: `services.Configure<MyOptions>(config.GetSection("My"))` plus `ValidateDataAnnotations().ValidateOnStart()`.

| Interface | Lifetime | Reloads? |
|---|---|---|
| `IOptions<T>` | Singleton | No reload |
| `IOptionsSnapshot<T>` | Scoped | Reload per request (can't inject into singletons) |
| `IOptionsMonitor<T>` | Singleton | `CurrentValue` + change notifications |

- Secrets: never in `appsettings.json` or code → user-secrets (dev), Key Vault / K8s secrets (your **Sealed Secrets** experience).
- Magic numbers in code that should be config (timeouts, batch sizes, limits).

## 7. Logging

```csharp
// ❌ string interpolation: no structured properties, always allocates
_logger.LogInformation($"Processed {count} events for {userId}");
// ✅ message template
_logger.LogInformation("Processed {Count} events for {UserId}", count, userId);
```
- Log the exception object: `LogError(ex, "…")`, not `LogError(ex.Message)`.
- No PII, tokens, passwords, or full request bodies.
- Correct levels: no `LogError` for expected validation failures, and no `LogInformation` inside a tight loop.
- High-performance: `[LoggerMessage]` source-generated logging.

## 8. Background work

- `BackgroundService.ExecuteAsync`: loop on `!stoppingToken.IsCancellationRequested`, **create a scope per iteration** for scoped services, catch and log exceptions **inside** the loop.
- .NET 6+: an unhandled exception in a `BackgroundService` **stops the host** by default (`BackgroundServiceExceptionBehavior.StopHost`).
- `PeriodicTimer` for intervals. Watch for overlapping runs and multiple replicas running the same job (needs a distributed lock or a single-instance job / queue).
- Long work should be idempotent and checkpointed.

## 9. Caching

- `IMemoryCache`:
  - unbounded keys (user input in the key) → memory leak, so set `SizeLimit` and expirations
  - per-instance only (several replicas = inconsistent data)
- Cache stampede: many requests miss at once. .NET 9+ `HybridCache` handles this.
- Never cache per-user/tenant data under a global key.

## 10. Testability smells

- `DateTime.UtcNow` hard-coded → inject `TimeProvider` (.NET 8+).
- `static` helpers with I/O; `new SqlConnection()` inside business logic.
- Business logic in controllers → move it to services.

## 11. Health, observability
- `AddHealthChecks()` with liveness/readiness (you know K8s probes).
- Correlation IDs / OpenTelemetry traces across services.
