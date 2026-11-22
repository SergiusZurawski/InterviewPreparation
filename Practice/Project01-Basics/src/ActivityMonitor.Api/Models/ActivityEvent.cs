namespace ActivityMonitor.Api.Models;

/// <summary>
/// A single usage event: a user performed an action on a data product.
/// </summary>
public class ActivityEvent
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    /// <summary>e.g. "Login", "Search", "Download".</summary>
    public string Action { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }

    /// <summary>Set by the nightly billing job.</summary>
    public bool IsBillable { get; set; }
}
