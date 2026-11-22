using UsageSync.Api.Models;

namespace UsageSync.Api.Services;

public interface ISyncStatusStore
{
    SyncJob Create(int tenantId, string vendorId);

    SyncJob? Get(Guid syncId);

    void Update(Guid syncId, Action<SyncJob> update);

    int RemoveOlderThan(TimeSpan age);
}

/// <summary>Thread-safe in-memory store for on-demand sync jobs.</summary>
public sealed class SyncStatusStore : ISyncStatusStore
{
    private Dictionary<Guid, SyncJob> _jobs = new();

    public SyncJob Create(int tenantId, string vendorId)
    {
        var job = new SyncJob
        {
            SyncId = Guid.NewGuid(),
            TenantId = tenantId,
            VendorId = vendorId,
            State = SyncJobState.Queued,
            CreatedUtc = DateTime.UtcNow
        };

        lock (_jobs)
        {
            _jobs[job.SyncId] = job;
        }

        return job;
    }

    public SyncJob? Get(Guid syncId)
    {
        return _jobs.TryGetValue(syncId, out var job) ? job : null;
    }

    public void Update(Guid syncId, Action<SyncJob> update)
    {
        if (_jobs.TryGetValue(syncId, out var job))
        {
            update(job);
        }
    }

    public int RemoveOlderThan(TimeSpan age)
    {
        lock (_jobs)
        {
            var cutoff = DateTime.UtcNow - age;
            var remaining = _jobs
                .Where(pair => pair.Value.CreatedUtc >= cutoff)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            var removed = _jobs.Count - remaining.Count;
            _jobs = remaining;
            return removed;
        }
    }
}
