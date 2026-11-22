# 05 · Testing & Debugging

> Your edge: 7+ years in test automation before development. Use it: *"Coming from QA automation, the first thing I ask is: would this test fail if the code were broken?"*

## 1. xUnit vs NUnit (TRG likely uses xUnit; you know NUnit)

| Concept | NUnit | xUnit |
|---|---|---|
| Test | `[Test]` | `[Fact]` |
| Parameterised | `[TestCase(1,2)]` | `[Theory]` + `[InlineData(1,2)]` / `[MemberData]` |
| Setup per test | `[SetUp]` | **Constructor** (new class instance per test) |
| Teardown | `[TearDown]` | `IDisposable` / `IAsyncLifetime` |
| Shared fixture | `[OneTimeSetUp]` | `IClassFixture<T>`, `ICollectionFixture<T>` |
| Parallelism | Opt-in | Collections run in parallel by default |
| Output | `TestContext` | `ITestOutputHelper` |

Assertions: FluentAssertions (`result.Should().Be(…)`). Note: v8 changed to a commercial licence, so teams may use Shouldly or the built-in asserts.
Mocks: **Moq** (`Mock<T>`, `Setup`, `Verify`) / **NSubstitute** (`Substitute.For<T>()`, `Received()`).

## 2. Test levels for an ASP.NET Core + EF Core app
- **Unit:** domain logic and services with dependencies mocked. Fast, but they **can't validate EF queries**.
- **Integration (most valuable here):** `WebApplicationFactory<Program>` runs the real pipeline in memory, with routing, auth, validation, serialization and DI.
```csharp
public class EventsApiTests(WebApplicationFactory<Program> f) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Get_events_of_other_tenant_returns_404()
    {
        var client = f.WithWebHostBuilder(b => b.ConfigureTestServices(s => {
            // swap DB to Testcontainers SQL Server, fake auth handler, fake external clients
        })).CreateClient();
        var resp = await client.GetAsync("/api/events/123");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
```
- **Database:** Testcontainers (real SQL Server in Docker) > SQLite in-memory > ❌ EF InMemory provider (no SQL translation, constraints, or transactions → false confidence).
- External HTTP: **WireMock.Net** (you already use WireMock).
- **Contract / E2E:** you know these from WebdriverIO and Selenium.

## 3. Spotting test gaps in a PR (checklist)
- [ ] New behaviour has tests at all?
- [ ] **Unhappy paths:** not found, invalid input, unauthorized / other tenant, external failure, timeout, cancellation.
- [ ] **Boundaries:** empty list, one item, page limits, date range edges, null optional fields.
- [ ] **Concurrency / idempotency:** double submit, duplicate message.
- [ ] Does the test **assert behaviour** or just that a mock was called?
- [ ] **Would it fail if the code were broken?** (mutation thinking: invert the condition mentally; does any test go red?)
- [ ] Is the test asserting the value it set up in the mock? (tautology)
- [ ] Hidden swallowed exceptions or `try/catch` in the test → false positives (**your Claims story**).
- [ ] Non-deterministic: `DateTime.Now`, random, ordering without `OrderBy`, shared state, `Task.Delay` sleeps.
- [ ] Test names describe the scenario: `Method_Condition_ExpectedResult`.
- [ ] EF queries tested against a relational DB.

## 4. AI-generated test smells
- Mocks the class under test, or mocks so much that nothing real runs.
- `Verify(x => x.Save(It.IsAny<…>()))` as the only assertion.
- Only happy-path tests; 10 near-duplicate tests instead of one `[Theory]`.
- Asserts `NotNull` only.
- Tests that encode the bug (generated from the implementation, not from requirements).

## 5. Debugging approach (for "how would you debug X?" questions)
1. **Reproduce**: get exact input, environment, version. Minimal repro.
2. **Observe**: logs with correlation IDs, traces (OpenTelemetry / App Insights), metrics. Look at what actually happened, not what the code says should happen.
3. **Hypothesise and bisect**: narrow down the layer (client → API → service → DB → external). `git bisect` for regressions.
4. **Tools:**
   - Visual Studio / Rider debugger: conditional breakpoints, tracepoints, "break on thrown exception", Immediate window
   - EF: `ToQueryString()`, SQL logging, SQL Server execution plans, Query Store
   - Runtime: `dotnet-counters` (thread pool, GC, exceptions/sec), `dotnet-trace`, `dotnet-dump` (hangs, memory), `dotnet-gcdump`
   - HTTP: browser dev tools, Fiddler, WireMock recordings
   - K8s: `kubectl logs/describe/exec`, events, probes
5. **Fix + regression test** that fails before the fix and passes after.
6. **Prevent**: was it a test gap, monitoring gap, or design flaw? Feed it back.

Thread-pool starvation signature: latency climbs while CPU stays low and the thread count keeps growing. Check `dotnet-counters` and look for `.Result` calls.
