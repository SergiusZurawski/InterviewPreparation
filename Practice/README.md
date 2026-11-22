# Code Review Practice Plan

**Goal:** build review "muscle memory" for C# / ASP.NET Core / EF Core, then rehearse the real format (an AI-written PR, reviewed with an AI tool, thinking out loud).
**Interview:** Thursday 26 Nov 2026. **Today:** Thursday 19 Nov 2026, so 7 days left.

## How each project works

```
Practice/
  ProjectNN-<Focus>/
    PR.md          ← the ticket + PR description. READ THIS FIRST.
    src/...        ← the code under review (it compiles, and it looks "polished", like AI output)
    MY-REVIEW.md   ← YOU write your findings here
    FEEDBACK.md    ← I create it after reading MY-REVIEW.md: what you found, what you missed, explanations
```

1. Read `PR.md`, then the code. Timebox it (the time is given in each PR.md).
2. Write findings in `MY-REVIEW.md` using the format there (location · severity · problem · impact · fix).
3. Tell me "Project N done". I read your review and write `FEEDBACK.md`:
   - each planted issue: ✅ found / 🟡 partial / ❌ missed, with an explanation and the fix
   - your extra findings: valid / debatable / false positive
   - severity calibration and how you phrased comments
   - patterns to drill next
4. Weak spots feed into the next project.

> There is no answer key in the repo, so you can't peek. I find the issues again from the code when I review.

## Projects

| # | Focus | Format | AI allowed? | Timebox | Planned day |
|---|---|---|---|---|---|
| **01** | **Basics:** obvious ASP.NET Core + EF Core mistakes (security, null, overposting, DI lifetimes, sync-over-async, swallowed exceptions, dates, integer math) | Small Web API, whole files | ❌ Solo | 30–40 min | Thu 19 |
| 02 | **Async, concurrency & C# fundamentals:** `async void`, fire-and-forget, `CancellationToken`, DbContext in `Task.WhenAll`, `HttpClient` misuse, singleton shared state, `BackgroundService` + scoped deps, boxing, multiple enumeration, string concat in loops, `lock` mistakes | Small API + background worker | ❌ Solo | 40 min | Fri 20 |
| 03 | **EF Core deep dive + raw SQL:** N+1, lazy loading, cartesian `Include`, tracking, `Skip/Take` without `OrderBy`, `SaveChanges` in a loop, missing transaction, concurrency tokens, `FromSqlRaw` vs `FromSql`, client evaluation, `Count()` vs `Any()`, a dangerous migration | Data layer + migration | ❌ Solo | 40 min | Sat 21 |
| 04 | **Tests, errors & performance:** false-positive tests, mocks asserting mocks, InMemory provider hiding bugs, missing edge cases, wrong status codes / ProblemDetails, idempotency, caching across tenants | API + test project | ⚠️ Optional, at the end, to compare | 45 min | Sun 22 |
| 05 | **Mock interview #1:** a realistic PR as a `.diff` against a "good" codebase, mixed issues | One markdown file (as they will share it) | ✅ Claude Code in an empty folder, narrate out loud | 45 min | Mon 23 |
| — | Fix gaps from the feedback on 01–05 and drill technical questions (`KnowledgeBase/09`) | — | — | — | Tue 24 |
| 06 | **Mock interview #2:** different domain, includes a fix-and-verify step | Same as 05 | ✅ | 45 min + debrief | Wed 25 |

For mocks 05–06, feedback also covers **how you briefed the AI**, which of its claims you checked or rejected, and the final verdict. Those are the interview criteria.

## Severity labels (from `KnowledgeBase/01`)
**Blocker** · **Should fix** · **Nit** · **Question** (ask the author)
