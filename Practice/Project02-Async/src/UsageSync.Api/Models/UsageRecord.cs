namespace UsageSync.Api.Models;

public class UsageRecord
{
    public long Id { get; set; }

    public int TenantId { get; set; }

    public string VendorId { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public string Product { get; set; } = string.Empty;

    public int Units { get; set; }

    public double UnitPrice { get; set; }

    public DateTime UsageDateUtc { get; set; }

    public DateTime SyncedAtUtc { get; set; }
}
