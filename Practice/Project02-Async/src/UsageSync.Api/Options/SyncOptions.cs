namespace UsageSync.Api.Options;

public sealed class SyncOptions
{
    public const string SectionName = "Sync";

    public int IntervalMinutes { get; set; } = 60;

    public int MaxParallelVendors { get; set; } = 4;

    public List<VendorOptions> Vendors { get; set; } = new();
}

public sealed class VendorOptions
{
    public string VendorId { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Resolved from Key Vault through configuration.</summary>
    public string ApiKey { get; set; } = string.Empty;
}
