using System.Net.Http.Json;

namespace UsageSync.Api.Infrastructure;

// Existing code, not part of PR #231.
public interface INotificationClient
{
    Task SendAsync(string message, CancellationToken cancellationToken);
}

public sealed class NotificationClient : INotificationClient
{
    public const string HttpClientName = "notifications";

    private readonly IHttpClientFactory _httpClientFactory;

    public NotificationClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendAsync(string message, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var response = await client.PostAsJsonAsync("hooks/ops-channel", new { text = message }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
