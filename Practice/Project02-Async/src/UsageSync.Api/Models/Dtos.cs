namespace UsageSync.Api.Models;

/// <summary>One usage line as returned by a vendor's /v2/usage endpoint.</summary>
public sealed record VendorUsageDto(
    int AccountId,
    string UserEmail,
    string Product,
    int Units,
    double UnitPrice,
    DateTime UsageDateUtc);

public sealed record SyncJobResponse(
    Guid SyncId,
    string VendorId,
    string State,
    DateTime CreatedUtc,
    DateTime? CompletedUtc,
    int RecordsImported);
