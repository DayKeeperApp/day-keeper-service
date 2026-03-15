using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using DayKeeper.UserEmulator.Metrics;

namespace DayKeeper.UserEmulator.Client;

public sealed class AttachmentClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly HttpClient _httpClient;
    private readonly MetricsCollector _metrics;

    public AttachmentClient(HttpClient httpClient, MetricsCollector metrics)
    {
        _httpClient = httpClient;
        _metrics = metrics;
    }

    public async Task<AttachmentResponse> UploadAsync(
        byte[] fileContent,
        string fileName,
        string contentType,
        Guid? taskItemId,
        Guid? calendarEventId,
        Guid? personId,
        string personaName,
        string archetypeName,
        CancellationToken ct)
    {
        using var form = BuildUploadForm(fileContent, fileName, contentType, taskItemId, calendarEventId, personId);
        return await PostFormAsync("api/v1/attachments", form, "attachments/upload", personaName, archetypeName, ct).ConfigureAwait(false);
    }

    public async Task<byte[]> DownloadAsync(
        Guid id,
        string personaName,
        string archetypeName,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var statusCode = 0;

        try
        {
            var response = await _httpClient.GetAsync($"api/v1/attachments/{id}", ct).ConfigureAwait(false);
            stopwatch.Stop();
            statusCode = (int)response.StatusCode;
            response.EnsureSuccessStatusCode();
            var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            RecordMetric("attachments/download", statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: false);
            return bytes;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            RecordMetric("attachments/download", statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: true);
            throw new InvalidOperationException($"attachments/download failed: {ex.Message}", ex);
        }
    }

    public async Task<AttachmentResponse> GetMetadataAsync(
        Guid id,
        string personaName,
        string archetypeName,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var statusCode = 0;

        try
        {
            var response = await _httpClient.GetAsync($"api/v1/attachments/{id}/metadata", ct).ConfigureAwait(false);
            stopwatch.Stop();
            statusCode = (int)response.StatusCode;
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<AttachmentResponse>(_jsonOptions, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Null response from attachments/metadata");
            RecordMetric("attachments/metadata", statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: false);
            return result;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            RecordMetric("attachments/metadata", statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: true);
            throw new InvalidOperationException($"attachments/metadata failed: {ex.Message}", ex);
        }
    }

    public async Task DeleteAsync(
        Guid id,
        string personaName,
        string archetypeName,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var statusCode = 0;

        try
        {
            var response = await _httpClient.DeleteAsync($"api/v1/attachments/{id}", ct).ConfigureAwait(false);
            stopwatch.Stop();
            statusCode = (int)response.StatusCode;
            response.EnsureSuccessStatusCode();
            RecordMetric("attachments/delete", statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: false);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            RecordMetric("attachments/delete", statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: true);
            throw new InvalidOperationException($"attachments/delete failed: {ex.Message}", ex);
        }
    }

    private static MultipartFormDataContent BuildUploadForm(
        byte[] fileContent,
        string fileName,
        string contentType,
        Guid? taskItemId,
        Guid? calendarEventId,
        Guid? personId)
    {
        var form = new MultipartFormDataContent();
        var fileBytes = new ByteArrayContent(fileContent);
        fileBytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(fileBytes, "file", fileName);

        if (taskItemId.HasValue)
        {
            form.Add(new StringContent(taskItemId.Value.ToString()), "taskItemId");
        }

        if (calendarEventId.HasValue)
        {
            form.Add(new StringContent(calendarEventId.Value.ToString()), "calendarEventId");
        }

        if (personId.HasValue)
        {
            form.Add(new StringContent(personId.Value.ToString()), "personId");
        }

        return form;
    }

    private async Task<AttachmentResponse> PostFormAsync(
        string path,
        MultipartFormDataContent form,
        string endpoint,
        string personaName,
        string archetypeName,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var statusCode = 0;

        try
        {
            var response = await _httpClient.PostAsync(path, form, ct).ConfigureAwait(false);
            stopwatch.Stop();
            statusCode = (int)response.StatusCode;
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<AttachmentResponse>(_jsonOptions, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Null response from {endpoint}");
            RecordMetric(endpoint, statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: false);
            return result;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            RecordMetric(endpoint, statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: true);
            throw new InvalidOperationException($"{endpoint} failed: {ex.Message}", ex);
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
