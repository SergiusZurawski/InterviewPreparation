# PR #212 — AM-412: Activity events API

**Author:** dev-ai-assistant · **Target branch:** `main` · **Reviewer:** you

## Ticket AM-412

As a client admin I want to record and query usage activity for my firm's users across the data products we subscribe to, so I can see which products are actually used.

### Acceptance criteria
1. `POST /api/activity` records an event (user email, product name, action). The **server** sets the timestamp. Billing flags are set by the billing job, **not** by clients.
2. `GET /api/activity?from=2026-11-01&to=2026-11-30` returns the caller's events in the date range. **Both dates are inclusive.** Newest first.
3. `GET /api/activity/{id}` returns one event. It returns **404** if the event doesn't exist.
4. `GET /api/activity/search?product=terminal` does a partial match on product name.
5. `GET /api/activity/summary?from=..&to=..` returns, per product: the event count and its **share of all events as a percentage**.
6. A caller must only ever see **their own tenant's** data.

### Team conventions (existing codebase)
- Auth is JWT bearer. The tenant comes from the `tenant_id` claim. Use the existing helper `User.GetTenantId()` in `Infrastructure/ClaimsPrincipalExtensions.cs`.
- All timestamps are stored in **UTC**.
- Secrets come from configuration or Key Vault, never from source code.
- Logs must not contain personal data (emails count as personal data).

## PR description (written by the author)

> Adds `ActivityEvent` entity, `ActivityDbContext` registration, `ActivityService` and `ActivityController` with the 5 endpoints from AM-412. Handles errors gracefully and logs every recorded event. Tested manually with Postman.

## Files in this PR

| File | Status |
|---|---|
| `src/ActivityMonitor.Api/Program.cs` | modified (DbContext + service registration) |
| `src/ActivityMonitor.Api/Models/ActivityEvent.cs` | new |
| `src/ActivityMonitor.Api/Models/Dtos.cs` | new |
| `src/ActivityMonitor.Api/Data/ActivityDbContext.cs` | new |
| `src/ActivityMonitor.Api/Services/ActivityService.cs` | new |
| `src/ActivityMonitor.Api/Controllers/ActivityController.cs` | new |
| `src/ActivityMonitor.Api/Infrastructure/ClaimsPrincipalExtensions.cs` | existing, **not changed** (for context) |

---

## Your task

- **Timebox:** 30–40 minutes. **No AI** for this one. It trains your own eye.
- There are **13 planted issues** across the PR, plus room for a few nits. Not every file has one.
- Write each finding in `MY-REVIEW.md`. Use the format there.
- Finish with a verdict: Approve / Approve with comments / Request changes.
- Tip: compare the code against the **acceptance criteria and conventions** above, line by line. Half the bugs are "the code doesn't do what the ticket says".
