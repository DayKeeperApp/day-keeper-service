using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using DayKeeper.UserEmulator.Metrics;

namespace DayKeeper.UserEmulator.Client;

public sealed class DayKeeperApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public HttpClient HttpClient { get; }

    public DayKeeperApiClient(string baseUrl, Guid tenantId)
    {
        var handler = new HttpClientHandler();

        if (baseUrl.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase)
            || baseUrl.StartsWith("https://127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
#pragma warning disable MA0039 // Local dev tool — accept dev certs for localhost only
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#pragma warning restore MA0039
        }

        HttpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
        };

        HttpClient.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
        HttpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<JsonElement> GraphQLAsync(
        string operationName,
        string query,
        IDictionary<string, object?>? variables,
        MetricsCollector metrics,
        string personaName,
        string archetypeName,
        CancellationToken ct)
    {
        var body = new { query, variables };
        var stopwatch = Stopwatch.StartNew();
        var statusCode = 0;

        try
        {
            var response = await HttpClient.PostAsJsonAsync("graphql", body, JsonOptions, ct).ConfigureAwait(false);
            stopwatch.Stop();
            statusCode = (int)response.StatusCode;

            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            var root = document.RootElement.Clone();

            if (HasGraphQLErrors(root, out var errorMessage))
            {
                RecordRequest(metrics, operationName, statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: true);
                throw new GraphQLException(errorMessage);
            }

            var data = root.TryGetProperty("data", out var dataElement) ? dataElement : root;

            if (HasMutationPayloadErrors(data, out var payloadError))
            {
                RecordRequest(metrics, operationName, statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: true);
                throw new GraphQLException(payloadError);
            }

            RecordRequest(metrics, operationName, statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: false);
            return data;
        }
        catch (GraphQLException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            stopwatch.Stop();
            RecordRequest(metrics, operationName, statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: true);
            throw new GraphQLException(ex.Message);
        }
        catch
        {
            stopwatch.Stop();
            RecordRequest(metrics, operationName, statusCode, stopwatch.ElapsedMilliseconds, personaName, archetypeName, isError: true);
            throw;
        }
    }

    private static bool HasGraphQLErrors(JsonElement root, out string errorMessage)
    {
        if (root.TryGetProperty("errors", out var errorsElement)
            && errorsElement.ValueKind == JsonValueKind.Array
            && errorsElement.GetArrayLength() > 0)
        {
            var first = errorsElement[0];
            errorMessage = first.TryGetProperty("message", out var msg)
                ? msg.GetString() ?? "Unknown GraphQL error"
                : "Unknown GraphQL error";
            return true;
        }

        errorMessage = string.Empty;
        return false;
    }

    private static bool HasMutationPayloadErrors(JsonElement data, out string errorMessage)
    {
        // HotChocolate mutation convention: data.mutationName.errors[]
        // Walk each property of the data element looking for payload-level errors
        if (data.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in data.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Object
                    && prop.Value.TryGetProperty("errors", out var errorsElement)
                    && errorsElement.ValueKind == JsonValueKind.Array
                    && errorsElement.GetArrayLength() > 0)
                {
                    var first = errorsElement[0];
                    errorMessage = first.TryGetProperty("message", out var msg)
                        ? msg.GetString() ?? "Mutation payload error"
                        : "Mutation payload error";
                    return true;
                }
            }
        }

        errorMessage = string.Empty;
        return false;
    }

    private static void RecordRequest(
        MetricsCollector metrics,
        string operationName,
        int statusCode,
        long latencyMs,
        string personaName,
        string archetypeName,
        bool isError)
    {
        metrics.Record(new RequestRecord(
            operationName,
            "GraphQL",
            statusCode,
            latencyMs,
            personaName,
            archetypeName,
            isError,
            DateTime.UtcNow));
    }
}
