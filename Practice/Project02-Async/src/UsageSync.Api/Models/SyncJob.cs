namespace UsageSync.Api.Models;

public enum SyncJobState
{
    Queued,
    Running,
    Succeeded,
    Failed
}

public class SyncJob
{
    public Guid SyncId { get; set; }

    public int TenantId { get; set; }

    public string VendorId { get; set; } = string.Empty;

    public SyncJobState State { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? CompletedUtc { get; set; }

    public int RecordsImported { get; set; }
}
