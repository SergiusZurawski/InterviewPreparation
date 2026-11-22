# Feedback — Project 02 (PR #231)

## Score

| | Count |
|---|---|
| ✅ Found | 1 |
| 🟡 Partial | 2 |
| ❌ Missed | 16 |
| Your extra findings | 5: 1 valid, 1 debatable nit, 3 false positives |

**Honest summary:** this was a harder project than 01, and you stopped early because you were tired, which is fine for practice. Your instincts went to the right places: the worker loop, the `Task.Run` and `CancellationToken.None`. But every finding has an **empty Severity, Impact and Fix**, the AC checklist was left blank, and there's no verdict. In the interview, a finding without "what breaks, for whom" is half a finding. Three of your findings came from reading the code without checking how C# actually runs it (`for(;;)`, `UtcNow - started`, line 41). Those are the habits to fix, more than the specific bugs.

One more thing: you **ran the app** and got a crash with a stack trace that pointed straight at planted issue #4 (`async void`). Then you didn't write it up. Turning evidence like that into a finding is exactly the skill the interview tests.

---

## Planted issues

### Worker (`Workers/UsageSyncWorker.cs`)

**1. ❌ Captive dependency: a scoped service is injected into the hosted singleton.** `UsageSyncWorker.cs:19`, registered in `Program.cs:30`, Blocker
- `AddHostedService` registers the worker as a **singleton**. Its constructor takes `IUsageSyncService`, which is **scoped**, and that service holds the scoped `UsageDbContext`.
- **Impact:** in Development, scope validation makes the app **fail at startup** ("Cannot consume scoped service ... from singleton"). In Production there's no validation, so one `DbContext` lives forever. Its change tracker grows with every sync (memory leak, slower and slower), it caches stale entities, and it gets used from several threads (see #5).
- **Fix:** inject `IServiceScopeFactory`. In each cycle: `using var scope = _scopeFactory.CreateScope(); var svc = scope.ServiceProvider.GetRequiredService<IUsageSyncService>();`
- Drill rule (from P01): **a service may only depend on services that live the same length or longer.** Singleton → scoped is the classic violation.

**2. 🟡 (weak, via your #4) Shutdown ignores the stopping token.** `UsageSyncWorker.cs:37` and `:49`, Should fix (AC3)
- `while (true)` never checks `stoppingToken`, and `Task.Delay(TimeSpan.FromMinutes(60))` has **no token**. On shutdown the worker sits in a 60-minute delay, the host waits for its shutdown timeout (30 s by default), then gives up. That breaks AC3 ("stop within 10 s") on every deployment.
- Your #4 pointed at line 41, but line 41 *does* pass `stoppingToken`. The `CancellationToken.None` is at line 57 (the notification). Right area, wrong line, and the main problem (the delay) wasn't named.
- **Fix:** `while (!stoppingToken.IsCancellationRequested)` and `await Task.Delay(interval, stoppingToken)`. Or use `PeriodicTimer` with `WaitForNextTickAsync(stoppingToken)`.

**3. ✅ (your #3) An exception in one cycle kills the worker, and the whole API.** `UsageSyncWorker.cs:41`, Blocker (AC1)
- You linked it to the AC, which is good. What's missing is the **impact**, and it's bigger than you wrote. Since .NET 6, an unhandled exception in `ExecuteAsync` uses `BackgroundServiceExceptionBehavior.StopHost`, so **the whole process stops**, including the API endpoints. One vendor returning a 500 three times takes the API offline.
- Also, inside `SyncAllAsync` a failure in one vendor fails the whole `Task.WhenAll`, so the AC's "the other vendors still sync" isn't met either.
- **Fix:** `try/catch` around each cycle (log it, then continue, but let `OperationCanceledException` through when stopping). Also `try/catch` per vendor inside `SyncAllAsync`.

**4. ❌ `async void NotifyCompleted`.** `UsageSyncWorker.cs:53`, Blocker
- **This is the crash you got when you ran the app.** Nobody can await an `async void` method, so its exception has no `Task` to go to. The runtime re-throws it on a thread-pool thread, which **crashes the process**. Your stack trace shows exactly that: `Task.<>c.<ThrowAsync>` → `ThreadPoolWorkQueue.Dispatch`.
- **Impact:** if Teams has a hiccup, the usage-sync service crashes and restarts, every hour.
- **Fix:** `private async Task NotifyCompletedAsync(...)`, awaited inside a `try/catch` (a notification is "nice to have", so log and continue), and pass `stoppingToken`.
- Rule: **`async void` is only for event handlers.** Seeing one anywhere else is an automatic comment.

### Sync service (`Services/UsageSyncService.cs`)

**5. ❌ `Task.WhenAll` on one shared `DbContext`.** `UsageSyncService.cs:43-45`, Blocker
- Every `SyncVendorAsync` uses the same `_db` at the same time. `DbContext` is **not thread-safe**, so EF throws *"A second operation was started on this context instance before a previous operation completed."* With the 2 stub vendors it might sometimes pass locally (timing), so "tested locally" proves nothing.
- **Fix:** one DI scope, and so one `DbContext`, **per vendor** (`IServiceScopeFactory` again).

**6. ❌ Unbounded parallelism: `MaxParallelVendors` is never used.** `UsageSyncService.cs:38-43`, Should fix (AC2)
- The doc comment says "bounded by MaxParallelVendors", but the code starts all vendors at once. That's pattern #4 from P01: **read the comment, then check the code.**
- **Fix:** `await Parallel.ForEachAsync(_options.Vendors, new ParallelOptions { MaxDegreeOfParallelism = _options.MaxParallelVendors, CancellationToken = ct }, async (vendor, ct) => { /* new scope per vendor */ });` or a `SemaphoreSlim(max)`.

**19. ❌ Interpolated log message with an email in it.** `UsageSyncService.cs:74`, Should fix (convention)
- Two problems. `$"..."` creates a new string every time (even when Debug logging is off), and the log is no longer structured (no `{Units}` / `{Product}` fields to search on). It also logs `UserEmail`, which is PII and breaks the convention.
- **Fix:** `_logger.LogDebug("Imported {Units} units of {Product}", item.Units, item.Product);` with no email.

### Vendor client (`Services/VendorUsageClient.cs`)

**7. ❌ `new HttpClient()` on every call.** `VendorUsageClient.cs:34`, Should fix (convention)
- Disposing an `HttpClient` doesn't free its socket right away (the socket stays in TIME_WAIT). If you create one per call, under load you run out of sockets (**socket exhaustion**). The convention says to use `IHttpClientFactory`, and the existing `NotificationClient` shows how.
- **Fix:** `AddHttpClient<IVendorUsageClient, VendorUsageClient>()` and take `HttpClient` in the constructor. Add Polly / `AddStandardResilienceHandler()` for the retries.

**8. ❌ `Thread.Sleep` inside an async method.** `VendorUsageClient.cs:48`, Should fix
- It **blocks a thread-pool thread** for 2–4 s, and you can't cancel it. With many vendors failing, it starves the thread pool, and that starves the API requests too.
- **Fix:** `await Task.Delay(TimeSpan.FromSeconds(2 * attempt), cancellationToken);`

**9. ❌ Retry catches everything, and the token isn't passed.** `VendorUsageClient.cs:37` and `:43`, Should fix
- `catch (Exception)` also retries **4xx** responses (the contract says they're permanent, so a bad API key gets 3 attempts every cycle). It also catches **`OperationCanceledException`**, so a shutdown turns into a "retry".
- `client.GetAsync(url)` doesn't get `cancellationToken`, so the HTTP call itself can't be cancelled.
- **Fix:** retry only on `HttpRequestException` / 5xx / 429 (check `response.StatusCode`). Use `when (!cancellationToken.IsCancellationRequested)`. Call `GetAsync(url, cancellationToken)`.

**Your #5 (❌ false positive): "infinite loop `for (var attempt = 1; ; attempt++)`, where's the exit?"**
- It has two exits. `return records` on success, and the exception filter `when (attempt < MaxAttempts)`: on the 3rd failure the filter is `false`, so the exception **isn't caught** and leaves the method. It's a common C# idiom. As a **Question** ("is the exit condition clear enough? a `while (attempt <= MaxAttempts)` would be easier to read") it's a fair Nit. As a bug it's wrong.
- Lesson: before calling something a bug, trace one path through it: "attempt 3 fails → filter false → exception propagates".

**Your #6 (debatable Nit): "URL path should be in config".**
- `/v2/usage` is part of the vendor API **contract**, not an environment setting. `BaseUrl` is already in config. Keeping the version in code is normal. You could ask about it, but it doesn't belong in the top findings. The real problems in this method are #7–#9.

### On-demand sync (`Controllers/SyncController.cs`)

**10. 🟡 (your #7 + #8) Fire-and-forget `Task.Run` using a request-scoped service.** `SyncController.cs:45-69`, Blocker
- You saw the right line twice. But your explanation ("it might never get executed") and your fix ("await it") are both wrong:
  - It *does* run. The problem is **what it uses**: `_syncService`, and its `DbContext`, belong to the **request scope**. Once the action returns 202, the scope is disposed, and the background task gets `ObjectDisposedException` (cannot access a disposed context). It's a race, so it sometimes works locally.
  - It's **not tracked by the host**. On deploy or restart it's killed mid-way, and the job stays "Running" forever.
  - `CancellationToken.None` means shutdown can't stop it (your #7, valid).
  - **"Await it" breaks AC4**, which asks for 202 *at once*.
- **Fix:** put the work on a queue. A `Channel<SyncRequest>` (singleton), the controller writes to it, and the `BackgroundService` reads from it and creates its own scope. That's the standard ASP.NET Core "queued background work" pattern.

**11. ❌ `GET /api/sync/{syncId}` doesn't check the tenant: IDOR.** `SyncController.cs:77`, Blocker (AC4)
- The job stores `TenantId`, but `GetStatus` never compares it with `User.GetTenantId()`. Anyone with a `syncId` (from logs, a shared link, a browser history) can see another firm's jobs. **This is the same miss as in P01:** "where does the tenant come from, and is it checked *here*?"
- **Fix:** `if (job is null || job.TenantId != User.GetTenantId()) return NotFound();` Return 404, not 403, so you don't reveal that the id exists.

**Your #1 (✅ valid extra, wrong label): "anyone can specify vendorId, IDOR".**
- It's not really an IDOR (no other tenant's data comes back). But there's a real problem nearby: **any tenant's user can start a vendor-wide sync that imports every tenant's data**, as often as they like. There's no rate limit and no check for a sync already running for that vendor, so two syncs at once could import the same records twice. Phrase it as: *"Should fix / Question: should on-demand sync need an admin role and be rate-limited or de-duplicated per vendor?"* Good instinct, keep asking "who is allowed to call this?".

### Status store (`Services/SyncStatusStore.cs`)

**12. ❌ "Thread-safe" store that isn't thread-safe.** `SyncStatusStore.cs:19-63`, Should fix / Blocker
- It's a singleton, used at the same time by requests, by `Task.Run` jobs and by the worker.
  - `Get` (`:42`) and `Update` (`:47`) read the `Dictionary` with **no lock**, while `Create` writes it. A plain `Dictionary` read during a write can throw or even **loop forever** (a well-known .NET failure).
  - `RemoveOlderThan` **replaces `_jobs` inside `lock (_jobs)`** (`:63`). The next caller locks the *new* object, so two threads can each "hold the lock" on different objects. Lock on a `private readonly object _gate = new();` and never reassign it.
  - `update(job)` changes a shared `SyncJob` that another thread may be reading.
- The doc comment says "Thread-safe". Same lesson as #6: comment vs code.
- **Fix:** `ConcurrentDictionary<Guid, SyncJob>` plus immutable records (`_jobs[id] = job with { State = ... }`). Question for the author: it's in-memory, so what happens with 2 instances or a restart?

### Export (`Controllers/UsageController.cs`)

**13. ❌ The `IQueryable` is enumerated 4 times.** `UsageController.cs:42, 51, 65, 66`, Should fix
- `rows` is a **query, not a list**. `Any()`, `foreach`, `Sum()` and `Count()` each send a **separate SQL query**. That's 4 round trips over 50k rows, and if data changes in between, the count and totals won't match the lines.
- **Fix:** `var rows = await query.AsNoTracking().ToListAsync(ct);` once, then work in memory. Or let SQL do the totals with a `GroupBy`.
- Drill: when you see `IEnumerable`/`IQueryable` used more than once, ask *"how many times does this execute?"*

**14. ❌ `csv +=` in a loop.** `UsageController.cs:53`, Should fix (AC5: 50k rows)
- Strings are immutable, so each `+=` copies the whole string so far. 50k rows means O(n²) copying, hundreds of MB of garbage and large-object-heap allocations, so the export is slow and memory-heavy.
- **Fix:** `StringBuilder`, or better, stream straight to the response (`Response.BodyWriter` / `StreamWriter`) so the whole file is never held in memory.

**15. ❌ Synchronous EF calls and no `CancellationToken`.** `UsageController.cs:24`, Should fix (AC7)
- The action is synchronous, so every DB call blocks a request thread. There's no token, so when the client disconnects, the 50k-row work carries on.
- **Fix:** `public async Task<IActionResult> Export(string month, CancellationToken ct)`. ASP.NET Core binds `ct` to `HttpContext.RequestAborted`. Then `ToListAsync(ct)`.

**16. ❌ `double` for money.** `UsageRecord.cs:17`, `Dtos.cs:9`, `UsageController.cs:52`, Should fix (AC6)
- `double` is binary floating point, so 0.1 + 0.2 ≠ 0.3. Summing 50k costs drifts by cents, and the grand total won't equal the sum of the product totals. AC6 says "exact".
- **Fix:** `decimal` everywhere, plus `entity.Property(r => r.UnitPrice).HasPrecision(18, 4)` in EF. Round at the end, and decide the rounding mode (`MidpointRounding.AwayFromZero` vs the default banker's rounding). That's a good Question for the author.

**17. ❌ `Hashtable`: boxing on every row.** `UsageController.cs:48-56`, Nit
- `Hashtable` stores `object`, so every `double` is **boxed** (a heap allocation) on write and unboxed on read, 50k times. It's also untyped, and the totals come out in random order.
- **Fix:** `Dictionary<string, decimal>`, sorted by product before writing.

**18. ❌ Month boundary includes midnight of next month.** `UsageController.cs:38`, Should fix (AC5)
- `monthEnd = monthStart.AddMonths(1)` = **1 Dec 00:00:00**, and `<=` includes it. Any usage stamped exactly at midnight shows up in **both** November's and December's export, so it gets double-charged. Pattern #3 from P01: **do the boundary maths.**
- **Fix:** a half-open range, `>= start && < end`.

---

## Your other findings

| # | Finding | Verdict |
|---|---|---|
| 1 | vendorId IDOR | ✅ valid concern (authorization / rate limit / duplicate syncs), wrong label. See under #11. |
| 2 | "reverse order of `DateTime.UtcNow - startedUtc`" | ❌ **False positive.** Duration = end − start. `UtcNow - startedUtc` is positive and correct. Reversing it would give a negative `TimeSpan`. |
| 4 | `CancellationToken.None`, worker line 41 | 🟡 Line 41 passes `stoppingToken`. Counted as weak credit for #2. |
| 5 | infinite `for(;;)` loop | ❌ False positive (see #9). Fine as a readability Question. |
| 6 | URL in config | Debatable Nit. |
| 7, 8 | `Task.Run` + `CancellationToken.None` | 🟡 Counted as #10. |

---

## What went wrong in the process

1. **The AC checklist was skipped.** The ACs pointed straight at 8 of the 19 issues: #3 (AC1), #6 (AC2), #2 (AC3), #11 (AC4), #13/#14/#18 (AC5), #16 (AC6), #15 (AC7). Two minutes of checklist would have found more than the 45 minutes of reading did.
2. **Severity, Impact and Fix were empty on all 8.** Even a one-liner per finding would do: *"Blocker: the API dies when one vendor fails. Fix: try/catch per cycle."*
3. **Unchecked claims.** Three false positives came from not tracing the code. Before you write "bug", run it in your head with one concrete value.
4. **You had runtime evidence and didn't use it.** The crash = #4.
5. **No verdict.** It's a clear **Request changes** (5–6 Blockers).
6. **Whole files weren't reviewed**: `UsageController`, `SyncStatusStore` and most of `VendorUsageClient` got no findings, and that's where 9 issues were. Do a quick pass over every file first, before going deep on one.

## A model comment (how one should look)

> **Blocker — `UsageSyncWorker.cs:53` `async void NotifyCompleted`**
> An exception here (e.g. Teams webhook down, or a missing `Notifications:WebhookBaseUrl` — I reproduced this by running the app, stack trace below) can't be observed by any caller, so the runtime rethrows it on the thread pool and the **whole process crashes**, taking the API down with it. Notifications are best-effort, so this should never be fatal.
> **Suggestion:** make it `async Task`, await it inside `try/catch`, log at Warning, pass `stoppingToken`.

## Patterns to drill (they go into Project 03+)

1. **Lifetime check for every injected dependency:** *singleton → scoped = captive.* For background code: *"who creates the scope?"*
2. **For every `async` path:** *who awaits it, who sees the exception, does the token reach the last call?* `async void`, `Task.Run` with `_ =`, `Thread.Sleep`, `.Result`, and a missing `ct` are all automatic comments.
3. **Shared state:** *is this object used by more than one thread?* Then: `Dictionary` → `ConcurrentDictionary`, `lock` on a `private readonly object`, never reassign it.
4. **Deferred execution:** an `IQueryable`/`IEnumerable` used twice runs twice.
5. **Tenant check on every by-id lookup** (the 2nd project in a row this was missed).
6. **Trace before you claim:** one concrete value through the code, before writing "bug".
