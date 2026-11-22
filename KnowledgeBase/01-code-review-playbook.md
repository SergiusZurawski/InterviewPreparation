# 01 · Code Review Playbook

## 1. Process for the live exercise (~35–45 min)

| Phase | Time | What you do | What you say out loud |
|---|---|---|---|
| **1. Orient** | 3–5 min | Read the PR description/ticket first, then skim the whole diff. Identify: purpose, entry points, data flow, what's new vs changed. | "Before I involve the AI I want my own mental model: this PR adds X, the entry point is Y, data flows Y → service → DbContext." |
| **2. Own pass** | 5–8 min | Quick pass with the checklist below. Note hypotheses with line refs. | "First suspicions: this query runs in a loop, and I don't see an ownership check on this endpoint." |
| **3. Brief the AI** | 2–3 min | Give context + constraints + output format (see [07](07-ai-assisted-review.md)). | "I'm telling it the stack, the intent, and asking for severity + line numbers + reasoning so I can verify." |
| **4. Triage AI output** | 8–10 min | For each AI finding: **confirm / reject / needs-evidence**. Merge with your list. | "It flags X — agree. It says Y is a race condition — I disagree because the context is scoped per request." |
| **5. Fix & verify** | 8–10 min | Pick top 2–3 issues, propose or AI-draft fixes, **review the fix like a new diff**. | "This fix introduces a new problem — it loads the whole table to dedupe. Let's push that to SQL instead." |
| **6. Verdict** | 2–3 min | Summarise: blockers, should-fix, nits, missing tests, questions for the author. Approve / request changes. | "Request changes: two blockers, three should-fix. Here's what I'd want tested before merge." |

**Rule:** never just read AI output aloud. Every finding passes through *your* judgment, and the interviewers want to hear that judgment.

---

## 2. Review checklist (in priority order)

### 🔴 Correctness
- Does it do what the ticket says? Any off-by-one, wrong comparison, inverted condition, wrong operator precedence?
- Null handling: `FirstOrDefault()` result used without check? `!` null-forgiving hiding a real null?
- Boundaries: empty collections, zero, negative, max values, page 0, page size 0 / 10 000.
- Time: `DateTime.Now` vs `UtcNow`, time zones, inclusive/exclusive range ends (`<=` on end date loses/duplicates rows).
- Money/quantities: `double` for money, rounding, integer division.
- String comparisons: culture, case (`ToLower()` comparisons vs `StringComparison.OrdinalIgnoreCase`).
- Idempotency: what happens if this request/job runs twice?
- State: shared mutable state in singletons/statics.

### 🔴 Data access (EF Core) → details in [02](02-ef-core-for-dapper-devs.md)
- N+1 queries, query in loop, `SaveChanges` in loop.
- `ToList()`/`AsEnumerable()` before `Where` → whole table loaded.
- Missing `AsNoTracking` on read-only paths; missing projection (`Select` to DTO).
- Unbounded queries (no paging, no `Take`); `Skip/Take` without `OrderBy`.
- Transactions: multi-step writes not atomic; read-modify-write races; no concurrency token.
- Raw SQL built by string interpolation/concatenation.
- Migrations: data loss, non-null column without default, missing index.

### 🔴 Security → [06](06-security-checklist.md)
- Authorization: `[Authorize]` present? **Is the resource owned by the caller / their tenant?** (IDOR)
- Input validation; overposting (binding directly to entities).
- Secrets in code/config; PII/tokens in logs; exception details returned to clients.

### 🟠 Async & concurrency → [03](03-aspnet-core-pitfalls.md), [04](04-csharp-dotnet-fundamentals.md)
- `.Result` / `.Wait()` / `async void` / fire-and-forget `Task.Run` in a request.
- `CancellationToken` not accepted / not passed through.
- DbContext used concurrently (`Task.WhenAll` over same context).
- Non-thread-safe collections in singletons.

### 🟠 Error handling & resilience
- Swallowed exceptions (`catch { }`, `catch (Exception) { return null; }`).
- `throw ex;` (loses stack) vs `throw;`.
- Wrong HTTP status codes (200 with error body, 500 for validation).
- External calls: timeouts, retries, `HttpClient` lifetime.
- Partial failure: what's the state after step 2 of 3 fails?

### 🟡 Performance
- Big-O inside loops (`List.Contains` in a loop → `HashSet`).
- Allocations in hot paths, string concatenation in loops.
- Caching: unbounded keys, per-user data in shared cache, stale data.
- Loading more columns/rows than needed.

### 🟡 Readability & maintainability
- Naming, method length, single responsibility, duplication.
- Magic numbers/strings → constants/options.
- Dead code, commented-out code, TODOs without tickets.
- Consistency with existing codebase conventions.
- Over-engineering (AI loves extra abstractions).

### 🟠 Tests (your strength, so lean on it) → [05](05-testing-and-debugging.md)
- Are there tests at all? Do they test **behaviour** or just mocks?
- Unhappy paths, boundaries, authorization failures, concurrency.
- **Would the test fail if the code were broken?** (your Claims story: false-positive tests)
- Integration test for the EF query (in-memory provider hides real SQL behaviour).

---

## 3. Severity labels (use them verbally)

| Label | Meaning | Example |
|---|---|---|
| **Blocker** | Wrong results, data loss, security hole, prod outage risk | IDOR, SQL injection, data corruption under concurrency |
| **Should fix** | Real problem, not immediately catastrophic | N+1, missing cancellation, swallowed exception |
| **Nit / suggestion** | Style, naming, minor readability | Rename variable, extract constant |
| **Question** | Need author/business context | "Should deleted users still appear in the report?" |

Asking questions is a **senior signal**: "I'd ask the author whether X is intended. If it isn't, this is a blocker."

## 4. Phrasing review comments

- Describe the **impact**, not just the rule: ❌ "Use AsNoTracking." ✅ "This is a read-only listing; tracking 10k entities adds memory and CPU for no benefit → `AsNoTracking()` + project to DTO."
- Offer the fix or the direction.
- Separate facts from opinions ("I'd prefer…" vs "This will fail when…").

## 5. Typical "AI-written PR" smells

- Looks polished, compiles, has XML docs, **but** subtle logic bugs.
- Catch-all `try/catch` that logs and returns `null`/empty.
- Unused parameters, unused `using`s, `CancellationToken` accepted but never passed.
- Tests that mock everything and assert on the mock's return value.
- Over-abstracted: interface + factory + strategy for one implementation.
- Mixed old/new APIs (e.g. Newtonsoft + System.Text.Json; `Startup.cs` patterns in minimal hosting).
- Invented or misused framework APIs.
- Inconsistent naming between files (AI generated each file separately).
- Comments that describe what the code *should* do, not what it does. **Check code against comments.**
