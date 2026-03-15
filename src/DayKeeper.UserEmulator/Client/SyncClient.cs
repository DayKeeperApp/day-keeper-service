using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using DayKeeper.UserEmulator.Metrics;

namespace DayKeeper.UserEmulator.Client;

public sealed class SyncClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly HttpClient _httpClient;
    private readonly MetricsCollector _metrics;

    public SyncClient(HttpClient httpClient, MetricsCollector metrics)
    {
        _httpClient = httpClient;
        _metrics = metrics;
    }

    public async Task<SyncPullResponse> PullAsync(
        long? cursor,
        Guid? spaceId,
        int? limit,
        string personaName,
        string archetypeName,
        CancellationToken ct)
    {
        var request = new SyncPullRequest(cursor, spaceId, limit);
        var stopwatch = Stopwatch.StartNew();
        var statusCode = 0;

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/sync/pull", request, JsonOptions, ct).ConfigureAwait(false);
            stopwatch.Stop();
            statusCode = (int)response.StatusCode;
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SyncPullResponse>(JsonOptions, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Null response from sync/pull");

            RecordMetric("sync/pull", statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: false);
            return result;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            RecordMetric("sync/pull", statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: true);
            throw new InvalidOperationException($"sync/pull failed: {ex.Message}", ex);
        }
    }

    public async Task<SyncPushResponse> PushAsync(
        IReadOnlyList<SyncPushEntry> changes,
        string personaName,
        string archetypeName,
        CancellationToken ct)
    {
        var request = new SyncPushRequest(changes);
        var stopwatch = Stopwatch.StartNew();
        var statusCode = 0;

        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/sync/push", request, JsonOptions, ct).ConfigureAwait(false);
            stopwatch.Stop();
            statusCode = (int)response.StatusCode;
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SyncPushResponse>(JsonOptions, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Null response from sync/push");

            RecordMetric("sync/push", statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: false);
            return result;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            RecordMetric("sync/push", statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: true);
            throw new InvalidOperationException($"sync/push failed: {ex.Message}", ex);
        }
    }

    private void RecordMetric(string endpoint, int statusCode, long latencyMs, string personaName, string archetypeName, bool isError)
    {
        _metrics.Record(new RequestRecord(
            endpoint,
            "REST",
            statusCode,
            latencyMs,
            personaName,
            archetypeName,
            isError,
            DateTime.UtcNow));
    }
}
