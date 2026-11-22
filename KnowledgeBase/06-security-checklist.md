# 06 · Security Checklist for Reviewing ASP.NET Core / EF Core Code

> You come from healthcare (HIPAA-style sensitivity), so security-by-default thinking is credible. Mention it.

| Risk | What to look for in the diff | Fix |
|---|---|---|
| **Broken access control / IDOR** (the most common blocker) | `GET /api/reports/{id}` loads by id **without checking owner/tenant**; missing `[Authorize]`; stray `[AllowAnonymous]`; role checks only in the UI | Filter by `TenantId`/`UserId` from **claims**, never from the request body/query; policies (`[Authorize(Policy="…")]`); global query filters; return 404 for other tenants' resources |
| **Injection** | `FromSqlRaw($"…{x}")`, string-built SQL in Dapper/ADO, dynamic `OrderBy` from user input, LDAP/command injection | Parameters; allow-list sort columns; `FromSql` interpolated |
| **Mass assignment / overposting** | Binding request directly to an entity; `db.Update(entity)` from the request | Input DTOs with only the allowed fields |
| **Sensitive data exposure** | Returning entities with internal fields; PII/tokens in logs; `ex.Message`/stack trace in responses; `EnableSensitiveDataLogging` in prod | Output DTOs; ProblemDetails; log redaction |
| **Secrets** | Connection strings, API keys in code/appsettings/tests committed | Key Vault / K8s secrets / user-secrets |
| **Authentication misconfig** | `ValidateIssuer/Audience/Lifetime = false`, custom JWT parsing, `RequireHttpsMetadata=false` in prod | Defaults on; use the framework |
| **CORS** | `AllowAnyOrigin()` with credentials, wildcard origins in prod | Explicit origins |
| **Insecure deserialization** | Newtonsoft `TypeNameHandling.All/Auto`; `BinaryFormatter` | System.Text.Json; no type-name handling |
| **SSRF** | Server fetches a URL supplied by the user | Allow-list hosts; no internal addresses |
| **Path traversal** | `Path.Combine(root, userFileName)` | `Path.GetFileName`, validate the resolved path stays under root |
| **DoS / resource exhaustion** | Unbounded page size, unbounded date range, huge uploads, regex without timeout, no rate limiting | Limits, `RequestSizeLimit`, `RegexOptions` + timeout, rate limiter |
| **Crypto** | `Random` for tokens, MD5/SHA1 for passwords, homemade crypto | `RandomNumberGenerator`; ASP.NET Identity hasher / PBKDF2 / Argon2 |
| **Open redirect** | `Redirect(returnUrl)` unchecked | `LocalRedirect` / `Url.IsLocalUrl` |
| **CSRF** (cookie auth, MVC/Razor) | Missing antiforgery on state-changing forms | `[ValidateAntiForgeryToken]` / auto validation |
| **Supply chain** | New NuGet packages: unknown, abandoned, vulnerable | `dotnet list package --vulnerable`; Dependabot |
| **Logging & audit** | Security events not logged / too much logged | Audit sensitive actions without PII |

## Multi-tenancy: probably relevant for TRG
Financial institution clients = tenants. **Every query must be tenant-scoped.** In review ask:
- Where does `TenantId` come from? (Claims ✅, header/body ❌)
- Is there a global query filter? Does any new code bypass it (`IgnoreQueryFilters`, raw SQL, `ExecuteUpdate`)?
- Are caches keyed per tenant?
- Do background jobs set tenant context correctly?

## How to phrase it
> "This is a blocker: the endpoint takes `customerId` from the query string and doesn't compare it with the caller's tenant claim, so any authenticated user can read any customer's activity data. I'd scope the query by the tenant from the token and return 404 otherwise, and add an integration test with two tenants."
