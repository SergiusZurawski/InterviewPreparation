# Feedback — Project 01 (PR #212)

## Scorecard

| Result | Count |
|---|---|
| ✅ Found (right problem + right reason) | **4** |
| 🟡 Partial (right line, wrong or missing reason) | **3** |
| ❌ Missed | **8** |
| ➕ Valid extra findings | **2** (no tests, no migration) |
| ⚠️ False positives | **3** (`UseRouting`, controller logger, `OrderByDescending`) |

I said "13 issues". The full list below has 15 because two pairs are closely related (auth + tenant isolation, swallowed exception + logging). Count it either way.

**Overall:** your instincts are good. Every ✅ is a classic, and you went to the right **lines** for SQL, `!` and input binding. The gap isn't knowledge. You **didn't use `PR.md` as a checklist**: **7 of the 8 misses are written down in the acceptance criteria and conventions** (UTC, secrets, personal data in logs, inclusive dates, newest first, billing flags, tenant claim). In the interview the ticket is your most powerful tool. Read it first and check the code against it line by line.

---

## All planted issues

### 1. ❌ Hardcoded production secret — Blocker
`Program.cs:16-17`: `UseSqlServer("Server=sql-prod-01...;Password=Tr4ck!ng2026...")`
- **Problem:** the production DB password is committed to source control. That breaks the convention "secrets come from configuration or Key Vault".
- **Impact:** anyone with repo access (or a leaked clone, CI logs or a fork) has prod DB credentials. Git history keeps them **forever**, so the password must now be **rotated**, not just removed.
- **Fix:** `options.UseSqlServer(builder.Configuration.GetConnectionString("ActivityDb"))`. Keep the value in user-secrets locally and in Key Vault / environment variables in prod.
- You looked at this exact line for your finding #7 but read it as "DbContext not initialized" (see the false positives section).

### 2. ✅ Singleton service holding a scoped DbContext — Blocker
`Program.cs:19`: `AddSingleton<IActivityService, ActivityService>()`

You were right. Here is the **why** you asked for:

| Lifetime | One instance per… | Typical use |
|---|---|---|
| `Singleton` | the whole app, for its lifetime, shared by **all requests at once** | stateless helpers, caches, config, `IHttpClientFactory` |
| `Scoped` | HTTP request | **`DbContext`**, unit-of-work, per-request user info |
| `Transient` | every time it's injected | lightweight stateless services |

- `AddDbContext` registers `ActivityDbContext` as **Scoped**, because a DbContext is **not thread-safe** and keeps a change tracker (a cache of every entity it has loaded).
- A singleton that takes a scoped dependency in its constructor **captures** that dependency forever. This is called a **captive dependency**.
- **Impact:**
  1. In Development, ASP.NET Core's scope validation throws at startup: *"Cannot consume scoped service 'ActivityDbContext' from singleton 'IActivityService'"*.
  2. In Production (validation is off by default), **one DbContext is shared by all concurrent requests**. You get `InvalidOperationException: A second operation was started on this context instance…`, and the change tracker grows without limit (memory leak, stale data).
- **Fix:** `AddScoped<IActivityService, ActivityService>()`. **Rule:** a service can only depend on services with the **same or a longer** lifetime.
- For the interview, put it as one sentence: *"Singleton → captive scoped DbContext → shared across concurrent requests, not thread-safe → Blocker. Register it as Scoped."*

### 3. ✅ No `[Authorize]`, so every endpoint is anonymous — Blocker
`ActivityController.cs:7-9`
- Right finding. Here is the memory refresh you asked for:
  - **Authentication** = *who are you?* It's set up as **middleware/services**: `AddAuthentication().AddJwtBearer()` + `app.UseAuthentication()`. There is **no** `[Authentication]` attribute.
  - **Authorization** = *are you allowed?* It's applied with the **`[Authorize]`** attribute (optionally `[Authorize(Policy = "...")]` / `Roles = "..."`). `[AllowAnonymous]` opts an endpoint out.
  - Configuring authentication does **not** protect anything by itself. Without `[Authorize]` (or a global `FallbackPolicy` / `RequireAuthorization()`), endpoints accept requests with no token.
- **Fix:** `[Authorize]` on the controller, or better, a global fallback policy so new controllers are secure by default.
- **Note:** you labelled this IDOR. Missing `[Authorize]` is **broken authentication/authorization**. IDOR is the next issue.

### 4. 🟡 Tenant taken from the query string: the real IDOR / cross-tenant leak — Blocker
`ActivityController.cs:27, 51` (`[FromQuery] int tenantId`), plus `GetById` and `SearchByProduct`, which have **no tenant filter at all**
- **Problem:** the caller *chooses* whose data to read: `GET /api/activity?tenantId=42`. The convention says the tenant comes from the JWT claim via `User.GetTenantId()`, and that helper is even shown in the PR, unused.
- **Impact:** any logged-in user of firm A can read firm B's activity by changing a number, or by iterating ids on `/api/activity/{id}`. For a B2B financial SaaS this is a **data breach** of client data. AC6 is violated.
- **Fix:** remove `tenantId` from the API surface. `var tenantId = User.GetTenantId();` in the controller, and pass it to **every** service method, including `GetById(tenantId, id)` and `Search(tenantId, ...)`. Optionally use an EF **global query filter** (`HasQueryFilter(e => e.TenantId == _tenant.Id)`) so a missing filter can't happen.
- 🟡 because you named IDOR but tied it to the wrong cause.

### 5. ❌ Overposting: the API binds directly to the entity — Blocker / Should fix
`ActivityController.cs:19`: `Record([FromBody] ActivityEvent activityEvent)`
- **Problem:** the client can send `Id`, `TenantId` and `IsBillable`, and they are saved as-is. AC1 says *"Billing flags are set by the billing job, not by clients."*
- **Impact:** a client can mark events non-billable (revenue loss), write events **into another tenant** (`TenantId`), or send an `Id` and cause a PK conflict.
- **Fix:** a request DTO with only the allowed fields: `record RecordActivityRequest(string UserEmail, string ProductName, string Action)`, with validation (`[Required]`, `[MaxLength]`). The server sets `TenantId` from the claim, plus `OccurredAt` and `IsBillable = false`.
- Your finding #4 pointed at **exactly this line** with "no validation". That was the right instinct, but "add validation" is too generic to be a review comment. Say **which** input is dangerous and **why**.

### 6. ❌ `DateTime.Now` instead of `DateTime.UtcNow` — Should fix
`ActivityService.cs:34`
- **Problem:** the convention says UTC. `Now` is the server's local time.
- **Impact:** wrong timestamps if servers run in different time zones (cloud servers often run on UTC, dev PCs don't), plus a 1-hour jump at DST changes. Date-range queries and the summary then miscount events around midnight and DST boundaries.
- **Fix:** `DateTime.UtcNow`, or better, inject `TimeProvider` (.NET 8) and call `_time.GetUtcNow()`, which also makes it testable. Consider `DateTimeOffset`.

### 7. ✅ `.Result`: sync-over-async — Should fix
`ActivityService.cs:37`: `_db.SaveChangesAsync().Result`
- Right finding. A few corrections:
  - It isn't really "the old way". The problem is **blocking a thread pool thread** while waiting on I/O. Under load, request threads all sit blocked, the pool starves, and latency explodes (**thread-pool starvation**). In older ASP.NET / UI apps it could even **deadlock**. In ASP.NET Core there's no SynchronizationContext, so no deadlock, but starvation is still there. `.Result` also wraps exceptions in `AggregateException`.
  - Your **fix** ("add exception handling") doesn't fix this. The fix is **async all the way**: `Task<ActivityEvent> RecordEventAsync(..., CancellationToken ct)` → `await _db.SaveChangesAsync(ct)`, and `async Task<IActionResult>` in the controller. Same for every other method (`ToListAsync`, `FirstOrDefaultAsync`) → Should fix.
- `var saved` is unused (nit).

### 8. ❌ Swallowed exception → `200 OK` with `null` — Should fix
`ActivityService.cs:45-48` + `ActivityController.cs:22`
- **Problem:** `catch (Exception) { return null; }`, and the controller returns `Ok(result)`. A DB failure becomes **HTTP 200 with an empty body**.
- **Impact:** the client thinks the event was saved, so **silent data loss**. Monitoring sees no 5xx. `LogError(ex.Message)` throws away the **stack trace** and inner exception (for EF, the real SQL error is in `InnerException`).
- **Fix:** remove the try/catch and let a global exception handler (`app.UseExceptionHandler()` + ProblemDetails) return 500 and log it. If you do catch, use `_logger.LogError(ex, "Failed to record event for tenant {TenantId}", tenantId)`.
- The PR description says *"Handles errors gracefully"*, which is a classic AI-PR smell. **"Gracefully" usually means "hides failures".**

### 9. ❌ Personal data (email) in logs — Should fix
`ActivityService.cs:39-41`: logs `activityEvent.UserEmail`
- **Problem:** the convention says "emails count as personal data". There's also a log line on **every** event (high volume).
- **Impact:** GDPR exposure. Logs get shipped to third-party tools and kept longer than the data itself, and right-to-be-forgotten requests can't reach them.
- **Fix:** log the event Id / tenant Id instead. Consider `LogDebug` for per-event logs.
- Your finding #5 ("logger absent in the controller") went in the opposite direction. The issue wasn't too *little* logging but logging the *wrong things*.

### 10. ✅ `.ToList()` before `.Where()`: the whole table is loaded into memory — Blocker at scale
`ActivityService.cs:58`
- Correct, and your fix (remove the line) is right. To complete the comment, add the **impact**: an activity table holds **millions of rows for all tenants**. Every request pulls them all over the network into memory, then filters in C#. That means OOM / timeouts, and the `(TenantId, OccurredAt)` index is never used.
- After the fix, `Where` → `OrderBy` → `ToListAsync` is translated into one SQL query. Also add `AsNoTracking()` (read-only) and **paging** (`Take`), because the list is otherwise unbounded.

### 11. ❌ Inclusive end date drops the last day — Should fix (correctness)
`ActivityService.cs:61`: `e.OccurredAt <= to`
- **Problem:** `?to=2026-11-30` binds to `2026-11-30 00:00:00`. `<= to` includes only events at exactly midnight, so **the whole of 30 Nov is lost**. AC2 says both dates are inclusive.
- **Fix:** `e.OccurredAt < to.Date.AddDays(1)` (half-open range). Even better, use `DateOnly` parameters. Validate `from <= to`.
- This is one of the most common real-world reporting bugs. Always ask: *"what's the time part of the end date?"*

### 12. ❌ Ascending sort, but the doc comment and AC say newest first — Should fix
`ActivityService.cs:62`: `.OrderBy(e => e.OccurredAt)`
- **Problem:** the comment on the method says "newest first" and AC2 says "Newest first". The code does oldest first.
- **Fix:** `OrderByDescending(e => e.OccurredAt)`.
- **AI-PR smell:** the comments describe what the code *should* do. **Always check code against its comments.**

### 13. 🟡 `FirstOrDefault()!` → `NullReferenceException` → 500 instead of 404 — Should fix
`ActivityService.cs:68` + `ActivityController.cs:39`
- You spotted the `!`. Here's what it means: it's the **null-forgiving operator**. It tells the compiler *"trust me, this is never null"* and silences the warning. It isn't a typo, and removing it alone just brings the warning back.
- **Impact:** an unknown id → `FirstOrDefault` returns `null` → `ToDto(null)` throws NRE → **500**. AC3 requires **404**.
- **Fix:** return `ActivityEvent?`, and in the controller `if (ev is null) return NotFound();`. (Also filter by tenant, see #4.)
- **Rule:** in a review, every `!` is a question: *"how do we know it's not null?"*

### 14. 🟡 SQL injection in `FromSqlRaw` — Blocker
`ActivityService.cs:76-77`
- You spotted the right line, but the reason was wrong:
  - *"SQL won't be tracked by EF"* isn't true. `FromSqlRaw` results **are** tracked by default.
  - *"string.Format"* would be **just as bad**. Any way of building SQL from strings is injectable.
- **Problem:** `product` from the query string is concatenated into SQL. `?product=' ; DROP TABLE Events; --` or `' OR 1=1 --` returns **every tenant's** data.
- **Fix, in order of preference:**
  1. Plain LINQ: `_db.Events.Where(e => e.TenantId == tenantId && e.ProductName.Contains(product))`. EF parameterizes it.
  2. `FromSql($"... LIKE {pattern}")` (EF 7+) / `FromSqlInterpolated`. This **looks** like interpolation, but EF turns each `{x}` into a **`DbParameter`**.
  3. `FromSqlRaw("... LIKE {0}", pattern)` or an explicit `SqlParameter`.
- **Remember:** `FromSqlRaw` + `$"..."` or `+` = injection. `FromSql` / `FromSqlInterpolated` + `$"..."` = safe.
- This one also has no tenant filter (see #4).

### 15. ❌ Integer division: every share is 0% — Should fix (correctness)
`ActivityService.cs:93`: `g.Count() / total * 100`
- **Problem:** `int / int` in C# is **integer division**. With 30 of 100 events: `30 / 100 = 0`, then `0 * 100 = 0`. Every product shows **0%** (except 100% when there's only one product).
- **Fix:** `(int)Math.Round(g.Count() * 100.0 / total)`, or make `SharePercent` a `decimal`/`double`. Multiplying first (`g.Count() * 100 / total`) also works but still truncates.
- Your finding #11 was **two lines below this** (`OrderByDescending`). Something felt off in that block, but the sort is fine. Do the arithmetic with sample numbers in your head: "30 events of 100 → what does this line return?"

---

## Your other findings

| # | Your finding | Verdict | Why |
|---|---|---|---|
| 2 | Missing `app.UseRouting()` | ⚠️ False positive | Since .NET 6, `WebApplication` (minimal hosting) adds routing automatically. `MapControllers()` works without it. You only need explicit `UseRouting()` to control its **order** relative to other middleware. **Mixing old `Startup.cs` patterns with new hosting** is an AI smell you *can* point out, but this code is correct. |
| 4 | "No input validation" | 🟡 Too vague | Right instinct on line 19 (→ #5 overposting). `[ApiController]` already returns 400 automatically for invalid models, but there are no rules (`[Required]`, `[MaxLength]`) to validate. For line 43: `product` is a non-nullable `string`, so a missing value gives a 400 automatically, but empty `""` matches everything. Next time, name the input, the bad value and the consequence. |
| 5 | No logger in the controller | ⚠️ False positive | Not an anti-pattern. Thin controllers often don't log. Errors are logged centrally by exception-handling middleware, and `[ApiController]` handles validation 400s. The real logging problems were in the service (#8, #9). |
| 7 | DbContext not initialized / no migrations | ⚠️ / ➕ Half valid | **DbContext:** it's registered by `AddDbContext` (`Program.cs:15`) and injected by DI into the service constructor. You never `new` it in ASP.NET Core. **Migrations: good catch!** The PR adds a new entity/table `Events` but no migration, so it would fail in every environment. I didn't plant it on purpose, but it's a **valid finding**. Phrase it as *"Should fix / Question: where's the migration for the `Events` table?"* That line was also the hardcoded secret (#1). |
| 11 | `OrderByDescending(p => p.EventCount)` might not work | ⚠️ False positive | It's LINQ-to-objects on an in-memory list after `GroupBy`, so it works fine. The bug was 2 lines above (#15). |
| 12 | No unit tests | ➕ Valid | Good. The PR says "tested manually with Postman", which deserves a comment. Be specific about **which** tests (see below). On "mock the DB": prefer **integration tests on a real provider** (SQLite or Testcontainers SQL Server). The InMemory provider would hide #10 and can't run `FromSqlRaw` (#14) at all. |

## Tests I'd ask for (to fill in that section next time)
- `GetEvents` with `to = 2026-11-30` returns an event at `2026-11-30T15:00` (would catch #11).
- Results are ordered newest first (#12).
- Summary with 3 events of product A and 1 of product B returns 75 / 25 (#15).
- `GET /{id}` with an unknown id returns 404 (#13).
- Tenant A's token cannot read tenant B's event by id, by list or by search (#4).
- POST with `IsBillable = true` / `TenantId = 99` in the body ignores them (#5).
- Search with `'` in the product name doesn't break the query (#14).
- Request without a token returns 401 (#3).

## How you wrote the review
- **Severity:** use the 4 labels (**Blocker / Should fix / Nit / Question**), not "medium/significant/moderate/high". Interviewers want to hear you prioritise.
- **Impact was blank in most findings.** That's the field that shows seniority. "Too much data" → "every request loads all tenants' rows into memory → OOM/timeouts at prod volume".
- **"Might" and "not sure":** honest, and that's good. Turn the doubt into a **Question** for the author or a quick check ("I'd confirm by enabling scope validation…"). In the interview, with AI available, that's exactly where you say *"let me verify this rather than guess"*, and they score you for it.
- **Fix ≠ unrelated fix:** #6's fix (exception handling) didn't address the `.Result` problem. Every fix should directly cancel the impact you described.
- No verdict. It should have been **Request changes** (5 blockers: secret, auth, tenant leak, overposting, SQL injection).

## Patterns to drill (they go into Project 02+)
1. **Ticket-driven review:** turn each AC and convention into a checklist item *before* reading the code.
2. **Multi-tenant isolation:** for every query ask *"where does tenantId come from, and is it filtered here?"*
3. **Do the arithmetic and boundary maths:** integer division, inclusive end dates, page 0 / empty list.
4. **Read the comment, then the code.** Do they disagree?
5. **Know the safe vs unsafe raw SQL APIs** (`FromSqlRaw` vs `FromSql`).
6. **DI lifetimes:** the "same or longer" rule.
