using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UsageSync.Api.Data;
using UsageSync.Api.Models;
using UsageSync.Api.Options;

namespace UsageSync.Api.Services;

public interface IUsageSyncService
{
    Task<int> SyncAllAsync(CancellationToken cancellationToken);

    Task<int> SyncVendorAsync(VendorOptions vendor, CancellationToken cancellationToken);
}

public sealed class UsageSyncService : IUsageSyncService
{
    private static readonly TimeSpan InitialLookback = TimeSpan.FromDays(30);

    private readonly UsageDbContext _db;
    private readonly IVendorUsageClient _vendorClient;
    private readonly SyncOptions _options;
    private readonly ILogger<UsageSyncService> _logger;

    public UsageSyncService(
        UsageDbContext db,
        IVendorUsageClient vendorClient,
        IOptions<SyncOptions> options,
        ILogger<UsageSyncService> logger)
    {
        _db = db;
        _vendorClient = vendorClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Syncs all configured vendors in parallel (bounded by <see cref="SyncOptions.MaxParallelVendors"/>)
    /// and returns the total number of imported records.
    /// </summary>
    public async Task<int> SyncAllAsync(CancellationToken cancellationToken)
    {
        var tasks = _options.Vendors.Select(vendor => SyncVendorAsync(vendor, cancellationToken));

        var counts = await Task.WhenAll(tasks);

        return counts.Sum();
    }

    public async Task<int> SyncVendorAsync(VendorOptions vendor, CancellationToken cancellationToken)
    {
        var lastSyncedUtc = await _db.UsageRecords
            .Where(r => r.VendorId == vendor.VendorId)
            .MaxAsync(r => (DateTime?)r.UsageDateUtc, cancellationToken)
            ?? DateTime.UtcNow - InitialLookback;

        var usage = await _vendorClient.GetUsageAsync(vendor, lastSyncedUtc, cancellationToken);

        var syncedAtUtc = DateTime.UtcNow;
        foreach (var item in usage)
        {
            _db.UsageRecords.Add(new UsageRecord
            {
                TenantId = item.AccountId,
                VendorId = vendor.VendorId,
                UserEmail = item.UserEmail,
                Product = item.Product,
                Units = item.Units,
                UnitPrice = item.UnitPrice,
                UsageDateUtc = item.UsageDateUtc,
                SyncedAtUtc = syncedAtUtc
            });

            _logger.LogDebug($"Imported {item.Units} units of {item.Product} for {item.UserEmail}");
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Vendor {VendorId}: imported {RecordCount} usage records", vendor.VendorId, usage.Count);
        return usage.Count;
    }
}
