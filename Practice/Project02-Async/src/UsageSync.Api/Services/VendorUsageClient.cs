using System.Net.Http.Json;
using UsageSync.Api.Models;
using UsageSync.Api.Options;

namespace UsageSync.Api.Services;

public interface IVendorUsageClient
{
    Task<IReadOnlyList<VendorUsageDto>> GetUsageAsync(VendorOptions vendor, DateTime sinceUtc, CancellationToken cancellationToken);
}

public sealed class VendorUsageClient : IVendorUsageClient
{
    private const int MaxAttempts = 3;

    private readonly ILogger<VendorUsageClient> _logger;

    public VendorUsageClient(ILogger<VendorUsageClient> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Fetches usage newer than <paramref name="sinceUtc"/>. Transient failures are retried with linear backoff.
    /// </summary>
    public async Task<IReadOnlyList<VendorUsageDto>> GetUsageAsync(VendorOptions vendor, DateTime sinceUtc, CancellationToken cancellationToken)
    {
        var url = $"{vendor.BaseUrl.TrimEnd('/')}/v2/usage?since={sinceUtc:yyyy-MM-ddTHH:mm:ssZ}";

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                client.DefaultRequestHeaders.Add("X-Api-Key", vendor.ApiKey);

                using var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var records = await response.Content.ReadFromJsonAsync<List<VendorUsageDto>>(cancellationToken: cancellationToken);
                return records ?? new List<VendorUsageDto>();
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                _logger.LogWarning(ex, "Vendor {VendorId} call failed on attempt {Attempt}/{MaxAttempts}, retrying",
                    vendor.VendorId, attempt, MaxAttempts);

                Thread.Sleep(TimeSpan.FromSeconds(2 * attempt));
            }
        }
    }
}
