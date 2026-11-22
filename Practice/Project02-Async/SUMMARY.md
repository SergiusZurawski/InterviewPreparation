# Project 02 — Summary (solo + AI review)

The full details are in two files:
- **Solo review:** [`FEEDBACK.md`](FEEDBACK.md), in this folder. It covers each of the 19 planted issues with the fix.
- **AI review:** `D:\WorkSpace\interviewPrep\AI-Review-02\PROMPT-FEEDBACK.md`. It covers your prompts and includes a 45-minute interview script.

## Solo review — 1 ✅ / 2 🟡 / 16 ❌

- You stopped early. Severity, Impact, Fix, the AC checklist and the verdict were all blank.
- 3 false positives came from not tracing the code:
  - `for (;;)` with an exception filter does have an exit.
  - `UtcNow - startedUtc` is the correct order.
  - Line 41 *does* pass the token.
- You ran the app and hit the `async void` crash (#4), but didn't write it up.
- The tenant check (IDOR in `GetStatus`) was missed for the **2nd project in a row**.
- Whole files got no findings: `UsageController`, `SyncStatusStore` and most of `VendorUsageClient`. Those files held 9 of the issues.
- **Key lesson:** the acceptance criteria pointed straight at 8 of the 19 issues. A 2-minute checklist would have beaten 45 minutes of reading.

## AI review — the AI found 19 / 19, plus ~6 extras

**What improved since AI-01**
- ✅ The **coverage matrix from the ACs** was your best prompt. It alone found everything.
- ✅ The **curl smoke test** made the AI run the app and **prove** 6 bugs:
  - `ObjectDisposedException` in the on-demand job
  - the cross-tenant job leak
  - Development startup failure (captive dependency)
  - the 2-vendor shutdown (`StopHost`)
  - duplicate imports
  - CSV defects
- ✅ Real extras: `since` drops milliseconds (duplicates), CSV/formula injection, the grand total doesn't match the lines.

**What's still missing**
- ❌ You didn't challenge the AI once.
- ❌ You missed **2 AI mistakes**:
  1. "`new HttpClient` per call ignores DNS changes" is backwards. That problem belongs to a *long-lived* client. Per-call creation causes socket exhaustion.
  2. "Banker's rounding makes 2.675 → 2.67" gives the wrong reason. The `double` 2.675 is 2.67499…, so it rounds down in *any* mode.
- ❌ You didn't limit output size. There were ~1,060 lines in total, including 83 recommended tests, which is too much to read or discuss.
- ❌ You set no boundaries before the AI ran things, and it created a LocalDB database and started Python servers.
- ❌ No top 5, no PR comments, no verdict in your own words. Those were steps 7, 8 and 11 of your own plan.

## Patterns to drill

1. **AC checklist first**, solo or with AI.
2. **Lifetimes:** singleton → scoped = captive dependency. *Who creates the scope in background code?*
3. **Async paths:** who awaits it, who sees the exception, does the token reach the last call? Watch for `async void`, `_ = Task.Run`, `Thread.Sleep` and a missing `ct`.
4. **Shared state:** `Dictionary` in a singleton → `ConcurrentDictionary`. Never reassign a lock object.
5. **Deferred execution:** an `IQueryable` used 4 times means 4 queries.
6. **Tenant check on every by-id lookup.**
7. **Trace one concrete value** through the code before you write "bug".

## Habits for Projects 05–06 (AI mocks)

1. Keep it: requirements → matrix first.
2. Keep it: verify by running. Set boundaries first (read-only `src/`, a scratch folder).
3. Limit the output in every prompt (max rows or bullets).
4. Challenge at least once, and catch at least one AI mistake **out loud**.
5. Finish with **top 5, PR comments and a verdict in your own words**, then let the AI critique it.
6. When the AI says "I didn't run / assumed", that's your next prompt.

## Cleanup

The AI left the `UsageSyncSmoke` database in LocalDB. The command to remove it is at the end of `AI-Review-02\ai_help\api-smoke-tests.md`.
