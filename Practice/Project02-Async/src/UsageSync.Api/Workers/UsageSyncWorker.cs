using Microsoft.Extensions.Options;
using UsageSync.Api.Infrastructure;
using UsageSync.Api.Options;
using UsageSync.Api.Services;

namespace UsageSync.Api.Workers;

public sealed class UsageSyncWorker : BackgroundService
{
    private static readonly TimeSpan JobRetention = TimeSpan.FromDays(1);

    private readonly IUsageSyncService _syncService;
    private readonly ISyncStatusStore _statusStore;
    private readonly INotificationClient _notifications;
    private readonly SyncOptions _options;
    private readonly ILogger<UsageSyncWorker> _logger;

    public UsageSyncWorker(
        IUsageSyncService syncService,
        ISyncStatusStore statusStore,
        INotificationClient notifications,
        IOptions<SyncOptions> options,
        ILogger<UsageSyncWorker> logger)
    {
        _syncService = syncService;
        _statusStore = statusStore;
        _notifications = notifications;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Usage sync worker started. Interval: {IntervalMinutes} min, vendors: {VendorCount}",
            _options.IntervalMinutes, _options.Vendors.Count);

        while (true)
        {
            var startedUtc = DateTime.UtcNow;

            var imported = await _syncService.SyncAllAsync(stoppingToken);
            var removedJobs = _statusStore.RemoveOlderThan(JobRetention);

            _logger.LogInformation("Sync cycle finished: {RecordCount} records imported, {RemovedJobs} old jobs removed",
                imported, removedJobs);

            NotifyCompleted(imported, DateTime.UtcNow - startedUtc);

            await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes));
        }
    }

    private async void NotifyCompleted(int imported, TimeSpan duration)
    {
        await _notifications.SendAsync(
            $"Usage sync finished: {imported} records in {duration.TotalSeconds:F0}s",
            CancellationToken.None);
    }
}
