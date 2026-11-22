using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UsageSync.Api.Infrastructure;
using UsageSync.Api.Models;
using UsageSync.Api.Options;
using UsageSync.Api.Services;

namespace UsageSync.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sync")]
public class SyncController : ControllerBase
{
    private readonly IUsageSyncService _syncService;
    private readonly ISyncStatusStore _statusStore;
    private readonly SyncOptions _options;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        IUsageSyncService syncService,
        ISyncStatusStore statusStore,
        IOptions<SyncOptions> options,
        ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _statusStore = statusStore;
        _options = options.Value;
        _logger = logger;
    }

    [HttpPost("{vendorId}")]
    public IActionResult StartSync(string vendorId)
    {
        var vendor = _options.Vendors
            .FirstOrDefault(v => string.Equals(v.VendorId, vendorId, StringComparison.OrdinalIgnoreCase));
        if (vendor is null)
        {
            return NotFound();
        }

        var job = _statusStore.Create(User.GetTenantId(), vendor.VendorId);

        _ = Task.Run(async () =>
        {
            _statusStore.Update(job.SyncId, j => j.State = SyncJobState.Running);
            try
            {
                var imported = await _syncService.SyncVendorAsync(vendor, CancellationToken.None);

                _statusStore.Update(job.SyncId, j =>
                {
                    j.State = SyncJobState.Succeeded;
                    j.RecordsImported = imported;
                    j.CompletedUtc = DateTime.UtcNow;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "On-demand sync {SyncId} for vendor {VendorId} failed", job.SyncId, vendor.VendorId);

                _statusStore.Update(job.SyncId, j =>
                {
                    j.State = SyncJobState.Failed;
                    j.CompletedUtc = DateTime.UtcNow;
                });
            }
        });

        return AcceptedAtAction(nameof(GetStatus), new { syncId = job.SyncId }, ToResponse(job));
    }

    [HttpGet("{syncId:guid}")]
    public ActionResult<SyncJobResponse> GetStatus(Guid syncId)
    {
        var job = _statusStore.Get(syncId);
        if (job is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(job));
    }

    private static SyncJobResponse ToResponse(SyncJob job) =>
        new(job.SyncId, job.VendorId, job.State.ToString(), job.CreatedUtc, job.CompletedUtc, job.RecordsImported);
}
