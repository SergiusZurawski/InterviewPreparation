# 02 · EF Core for Dapper Developers

> Your angle in the interview: *"Day to day I use Dapper, so I always think about the actual SQL being sent. With EF Core I review by asking: what SQL does this generate, how many round-trips, how many rows and columns, and what is being tracked?"* That's an honest and strong position.

---

## 1. Mental model: Dapper vs EF Core

| Concept | Dapper | EF Core |
|---|---|---|
| Unit of work | You manage `IDbConnection`/`IDbTransaction` | `DbContext` **is** the unit of work |
| SQL | You write it | LINQ → translated to SQL (provider) |
| Change tracking | None. You write `UPDATE` yourself | `DbContext` snapshots loaded entities, `SaveChanges()` diffs and writes |
| Identity | Every query = new objects | Identity map: same key → same instance (when tracked) |
| Execution | `QueryAsync` executes immediately | `IQueryable` is **deferred** until materialised |
| Relationships | Multi-mapping / split queries manually | Navigations + `Include` / projections |
| Schema | Scripts / DbUp etc. | Migrations generated from model |
| Lifetime | Connection per operation | `DbContext` **scoped** (per request), **not thread-safe** |

### Key types
- `DbContext`: session with the DB; holds `ChangeTracker`.
- `DbSet<T>`: queryable table; `IQueryable<T>`.
- `OnModelCreating` / `IEntityTypeConfiguration<T>`: fluent mapping (keys, indexes, precision, relations).
- Registration: `services.AddDbContext<AppDbContext>(o => o.UseSqlServer(cs));` → **scoped**.
- `IDbContextFactory<T>` (`AddDbContextFactory`): create short-lived contexts in singletons, background services, Blazor, parallel work.
- `AddDbContextPool`: reuses context instances (perf). Don't keep state in the context.

---

## 2. When does the query run? (IQueryable vs IEnumerable)

The query is **built** while you compose an `IQueryable`, and **executed** when you materialise it:
`ToList/ToListAsync`, `ToArray`, `First/Single/Any/Count/Sum/Max(...)`, `foreach`, `AsEnumerable()` followed by enumeration.

```csharp
// ❌ Loads the ENTIRE table, then filters in memory
var active = db.Users.ToList().Where(u => u.IsActive);

// ❌ Same problem, less obvious: AsEnumerable switches to LINQ-to-Objects
var active = db.Users.AsEnumerable().Where(u => u.IsActive).ToList();

// ❌ Parameter typed as IEnumerable<T> silently pulls everything
IEnumerable<User> Filter(IEnumerable<User> src) => src.Where(u => u.IsActive);

// ✅ Filter in SQL, project only needed columns, no tracking
var active = await db.Users
    .Where(u => u.IsActive)
    .Select(u => new UserDto(u.Id, u.Name))
    .ToListAsync(ct);
```

**Client evaluation:** since EF Core 3.0, if a `Where`/`OrderBy` can't be translated (e.g. calls your C# method), EF **throws** at runtime, *except* in the final `Select` projection where client code is allowed. AI code often calls a custom method inside `Where` → runtime exception, not a compile error.

**Debug the SQL:** `query.ToQueryString()`, or `optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information)`. `EnableSensitiveDataLogging()` only in dev (it logs parameter values = PII).

---

## 3. The classic pitfalls (what a planted-bug PR will contain)

### 3.1 N+1 queries
```csharp
// ❌ 1 query for orders + N queries for customers (explicit loop query)
var orders = await db.Orders.ToListAsync(ct);
foreach (var o in orders)
    o.CustomerName = (await db.Customers.FindAsync(o.CustomerId)).Name;

// ❌ Also N+1 with lazy loading (UseLazyLoadingProxies + virtual navs)
foreach (var o in orders) Console.WriteLine(o.Customer.Name);

// ✅ One query, joined in SQL
var rows = await db.Orders
    .Select(o => new { o.Id, CustomerName = o.Customer.Name })
    .ToListAsync(ct);
// or: .Include(o => o.Customer) if you need entities
// or: batch load: var ids = ...; db.Customers.Where(c => ids.Contains(c.Id))
```

### 3.2 Cartesian explosion
Multiple collection `Include`s in one query → JOIN multiplies rows (orders × lines × payments).
```csharp
db.Orders.Include(o => o.Lines).Include(o => o.Payments)   // row explosion
    .AsSplitQuery()                                          // ✅ separate SQL per collection
```
Trade-off: split queries = multiple round-trips + no single-snapshot consistency.

### 3.3 Tracking on read-only paths
```csharp
// ✅ Read-only listing → no change-tracking cost
db.Events.AsNoTracking().Where(...).ToListAsync(ct);
```
Projections into DTOs (`Select(x => new Dto(...))`) are not tracked anyway, and they're usually the best option for APIs.
`AsNoTrackingWithIdentityResolution()` → no tracking but de-duplicates instances.

### 3.4 `SaveChanges` in a loop
```csharp
// ❌ N round-trips, N transactions, partial failure leaves half the data
foreach (var e in items) { db.Add(e); await db.SaveChangesAsync(ct); }

// ✅ one SaveChanges: batched statements, ONE implicit transaction
db.AddRange(items);
await db.SaveChangesAsync(ct);
```

### 3.5 Bulk updates/deletes (EF Core 7+)
```csharp
// ❌ load 50k rows to change one column
// ✅ single SQL statement, bypasses change tracker
await db.Events.Where(e => e.CreatedUtc < cutoff).ExecuteDeleteAsync(ct);
await db.Users.Where(u => u.LastLogin < cutoff)
    .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false), ct);
```
Caveat: tracked entities in the current context aren't updated, and the call runs outside `SaveChanges`'s transaction unless you open one.

### 3.6 Raw SQL & injection
```csharp
// ❌ SQL injection: string built before EF sees it
db.Users.FromSqlRaw($"SELECT * FROM Users WHERE Name = '{name}'");
db.Users.FromSqlRaw("SELECT * FROM Users WHERE Name = '" + name + "'");

// ✅ Parameterised (FormattableString → DbParameters)
db.Users.FromSql($"SELECT * FROM Users WHERE Name = {name}");          // EF 7+
db.Users.FromSqlInterpolated($"SELECT * FROM Users WHERE Name = {name}");
db.Users.FromSqlRaw("SELECT * FROM Users WHERE Name = {0}", name);
db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM Users WHERE ...");  // EF 7/8+
```
**Gotcha:** `FromSqlRaw($"...{x}")` compiles fine and *looks* like the safe version. This is a favourite planted bug.

### 3.7 DbContext lifetime & threading
```csharp
// ❌ Captive dependency: singleton holds a scoped DbContext forever
services.AddSingleton<ReportCache>();   // ctor(AppDbContext db)

// ❌ BackgroundService injecting DbContext directly
public class Worker(AppDbContext db) : BackgroundService { ... }

// ✅ create a scope per unit of work
public class Worker(IServiceScopeFactory scopes) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken ct) {
        while (!ct.IsCancellationRequested) {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            ...
        }
    }
}
// ✅ or IDbContextFactory<AppDbContext>: await using var db = await factory.CreateDbContextAsync(ct);

// ❌ Concurrent operations on one context → InvalidOperationException
// "A second operation was started on this context instance before a previous operation completed"
await Task.WhenAll(db.A.ToListAsync(), db.B.ToListAsync());
```

### 3.8 Concurrency (read-modify-write)
```csharp
// ❌ Two requests read Balance=100, both write 100-10 → lost update
var acc = await db.Accounts.FindAsync(id);
acc.Balance -= amount;
await db.SaveChangesAsync();
```
Fixes:
- **Optimistic concurrency:** `[Timestamp] public byte[] RowVersion { get; set; }` (or `.IsRowVersion()`); EF adds `WHERE RowVersion = @orig` and throws `DbUpdateConcurrencyException` → retry / 409 Conflict.
- Atomic SQL: `ExecuteUpdateAsync(s => s.SetProperty(a => a.Balance, a => a.Balance - amount))`.
- Unique index for "check-then-insert" races (`if (!await db.X.AnyAsync(...)) db.Add(...)` is a race; the DB constraint is the real guard; catch `DbUpdateException`).

### 3.9 Transactions
- One `SaveChanges` = one transaction automatically.
- Multiple `SaveChanges` / mixing `ExecuteUpdate` / raw SQL that must be atomic → explicit:
```csharp
await using var tx = await db.Database.BeginTransactionAsync(ct);
... await db.SaveChangesAsync(ct); ... await db.SaveChangesAsync(ct);
await tx.CommitAsync(ct);
```
- With `EnableRetryOnFailure()` (retrying execution strategy), a user transaction must be wrapped in `db.Database.CreateExecutionStrategy().ExecuteAsync(...)`, otherwise EF throws.
- External side effects (send email, publish message) inside a DB transaction are not rolled back. Mention the **outbox pattern**.

### 3.10 Paging
```csharp
// ❌ No OrderBy → nondeterministic pages; EF warns
db.Events.Skip(page * size).Take(size)
// ❌ Unvalidated size → ?size=1000000
// ✅
db.Events.OrderBy(e => e.Timestamp).ThenBy(e => e.Id)   // unique tiebreaker
    .Skip((page - 1) * size).Take(Math.Clamp(size, 1, 100))
```
Large tables → **keyset pagination** (`Where(e => e.Id > lastId).OrderBy(e => e.Id).Take(n)`), because OFFSET gets slow when the offset is large.

### 3.11 Cheap vs expensive checks
- `Any()` not `Count() > 0`.
- `Count` + separate fetch = two queries (fine for paging, but know it).
- `Find(id)` checks the change tracker first (may skip the DB); `FirstOrDefault(x => x.Id == id)` always queries.
- `Single` throws on >1; use it when duplicates mean corrupted data. `First` without `OrderBy` = arbitrary row.

### 3.12 Updating entities from DTOs
```csharp
// ❌ Overposting + marks ALL columns modified
db.Users.Update(dtoMappedToEntity);  // client can set IsAdmin, TenantId...

// ✅ load → apply allowed fields → save (only changed columns written)
var u = await db.Users.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant, ct);
if (u is null) return NotFound();
u.Name = dto.Name;
await db.SaveChangesAsync(ct);
```

### 3.13 `Contains` with lists
`ids.Contains(x.Id)` → `IN (...)` or (EF 8+ on SQL Server) an `OPENJSON` parameter. Very large lists can hit parameter limits or produce bad plans; the exact translation depends on the EF version. Mention chunking or a temp/TVP table for huge lists.

### 3.14 Global query filters
`HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantId)` gives soft delete and multi-tenancy. In review, watch for:
- `IgnoreQueryFilters()` used without reason → leaks deleted/other-tenant data.
- `ExecuteUpdate`/raw SQL don't apply filters the same way (raw SQL ignores them).

### 3.15 Model/mapping issues
- `decimal` without precision → EF warning; SQL Server default `decimal(18,2)` truncates silently. Use `HasPrecision(18,4)`.
- `string` without max length → `nvarchar(max)` (can't be indexed efficiently).
- Missing index on foreign keys / filter columns used by the new query (`HasIndex(e => new { e.TenantId, e.Timestamp })`).
- Cascade delete defaults for required relationships: deleting the parent deletes the children. Is that intended?
- `DateTime` stored without kind → use UTC consistently or `DateTimeOffset`.

### 3.16 Migrations (if the PR includes one, read it!)
- Renames detected as **drop + add** → data loss.
- New non-nullable column on an existing table without a default → fails or fills with zero values.
- Index creation / column type change on a huge table → long locks.
- Data migration mixed with schema change; no down migration.
- `db.Database.Migrate()` at app startup with multiple replicas (Kubernetes) → race. Prefer migration bundles/scripts in the pipeline.

---

## 4. Performance knobs (to mention, not to lead with)
- Projection > Include for read APIs.
- `AsNoTracking` default for queries: `UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)`.
- `AddDbContextPool`; compiled queries (`EF.CompileAsyncQuery`) for hot paths.
- Raw SQL / Dapper alongside EF for complex reporting queries is a **legitimate** choice. Your Dapper experience is an asset here.

## 5. Testing EF code
- **InMemory provider: avoid for testing queries.** It is not relational: no SQL translation errors, no transactions, no constraints, different case-sensitivity. Tests pass and production fails. (Microsoft discourages it.)
- SQLite in-memory: closer, but a different SQL dialect.
- **Best:** Testcontainers with real SQL Server, or a repository abstraction plus integration tests.

## 6. 60-second answer: "How do you review EF Core code?"
> "I look at what SQL it produces and how many times it hits the database. Specifically: is filtering and paging done in SQL or after a `ToList`? Are there queries inside loops (N+1)? Are read paths `AsNoTracking` and projected to DTOs? Is `SaveChanges` batched, and are multi-step writes atomic? Is there concurrency protection for read-modify-write? Is raw SQL parameterised? Is the DbContext used within its scoped lifetime and never concurrently? And if there's a migration, could it lose data or lock a big table? Coming from Dapper, I check the generated SQL with `ToQueryString` or logging rather than trusting the LINQ."
