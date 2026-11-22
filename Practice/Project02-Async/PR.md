# PR #231 — AM-431: Vendor usage sync worker + usage export

**Author:** dev-ai-assistant · **Target branch:** `main` · **Reviewer:** you

## Ticket AM-431

As a client admin I want usage and cost data pulled automatically from our data vendors' APIs, and I want to export it monthly, so I can recharge costs to the business units that use each product.

### Acceptance criteria
1. A background worker syncs usage from **every** vendor in `Sync:Vendors` every `Sync:IntervalMinutes`. **One vendor failing must not stop the other vendors or the worker.** The failed vendor is simply retried on the next cycle.
2. Vendors are synced in parallel, **at most `Sync:MaxParallelVendors` (default 4) at a time**.
3. When the host shuts down (every deployment), the worker must **stop within 10 seconds**.
4. `POST /api/sync/{vendorId}` starts an on-demand sync and returns **202 Accepted** at once with a `syncId`. `GET /api/sync/{syncId}` returns that job's status. A caller can only see sync jobs that **their own tenant** started.
5. `GET /api/usage/export?month=2026-11` returns a CSV of the caller's tenant usage for that **calendar month**: one line per record (user, product, units, cost), then per-product totals, then the row count and grand total. Big tenants have **~50,000 rows a month**.
6. Costs are money. They must be exact and rounded to 2 decimal places.
7. If the client disconnects or the request times out, the export must stop its database work.

### Team conventions (existing codebase)
- Outgoing HTTP calls go through `IHttpClientFactory`. See the existing `Infrastructure/NotificationClient.cs`.
- `UsageDbContext` is registered as scoped (the default for `AddDbContext`).
- Logging uses message templates (structured logging). Logs must not contain personal data, and emails count as personal data.
- The tenant comes from `User.GetTenantId()`.
- All timestamps are stored in UTC.
- Vendor API contract: `GET /v2/usage?since=<utc>` returns records **strictly after** `since`. `429` and `5xx` are transient and may be retried. Other `4xx` codes are permanent (for example a bad API key), so retrying them is pointless.

## PR description (written by the author)

> Adds `UsageSyncWorker` (a `BackgroundService`) that syncs all vendors in parallel for speed, a `VendorUsageClient` with retry and backoff, and `UsageSyncService` that saves the records. On-demand sync runs in the background, so the API returns instantly. Job status is kept in a thread-safe in-memory store and cleaned up after 24h. A Teams notification is sent after every cycle. Adds the CSV export endpoint. Tested locally against 2 vendor stubs.

## Files in this PR

| File | Status |
|---|---|
| `src/UsageSync.Api/Program.cs` | modified (registrations) |
| `src/UsageSync.Api/Options/SyncOptions.cs` | new |
| `src/UsageSync.Api/Models/UsageRecord.cs` | new |
| `src/UsageSync.Api/Models/SyncJob.cs` | new |
| `src/UsageSync.Api/Models/Dtos.cs` | new |
| `src/UsageSync.Api/Data/UsageDbContext.cs` | new |
| `src/UsageSync.Api/Services/VendorUsageClient.cs` | new |
| `src/UsageSync.Api/Services/UsageSyncService.cs` | new |
| `src/UsageSync.Api/Services/SyncStatusStore.cs` | new |
| `src/UsageSync.Api/Workers/UsageSyncWorker.cs` | new |
| `src/UsageSync.Api/Controllers/SyncController.cs` | new |
| `src/UsageSync.Api/Controllers/UsageController.cs` | new |
| `src/UsageSync.Api/Infrastructure/ClaimsPrincipalExtensions.cs` | existing, **not changed** (for context) |
| `src/UsageSync.Api/Infrastructure/NotificationClient.cs` | existing, **not changed** (for context) |
| `src/UsageSync.Api/appsettings.json` | existing, **not changed** (for context; real values come from Key Vault / environment) |

---

## Your task

- **Timebox:** 45 minutes. **No AI** for this one.
- There are **19 planted issues**. Some are big, some are small. Not every file has one.
- Write each finding in `MY-REVIEW.md` using the format there. **Fill in Impact** every time: who gets hurt, and when.
- Finish with a verdict: Approve / Approve with comments / Request changes.

### Tips (from your Project 01 feedback)
1. **Before reading any code**, turn each AC and convention above into a checklist line in `MY-REVIEW.md`. Then tick them off.
2. For every query, ask: *where does the tenant come from, and is it checked here?*
3. Do the boundary maths: what is `monthEnd` exactly, and is it included?
4. Read each doc comment and the PR description, then check the code really does what they claim.
5. For every object, ask: *what is its lifetime, and who else uses it at the same time?* (singleton vs scoped, one thread vs many)
6. For every `async` path, ask: *who awaits this, who sees its exception, and does the cancellation token reach the end?*
