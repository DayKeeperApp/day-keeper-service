using System.Collections.Concurrent;
using HdrHistogram;

namespace DayKeeper.UserEmulator.Metrics;

public sealed class MetricsCollector
{
    private readonly ConcurrentQueue<RequestRecord> _records = new();
    private readonly LongHistogram _latencyHistogram = new(1, 60_000_000, 3);
    private readonly Lock _histogramLock = new();
    private readonly DateTime _startTime = DateTime.UtcNow;

    private long _totalRequests;
    private long _totalErrors;
    private long _graphQLRequests;
    private long _restSyncRequests;
    private long _restAttachmentRequests;

    public long TotalRequests => Interlocked.Read(ref _totalRequests);
    public long TotalErrors => Interlocked.Read(ref _totalErrors);
    public long GraphQLRequests => Interlocked.Read(ref _graphQLRequests);
    public long RestSyncRequests => Interlocked.Read(ref _restSyncRequests);
    public long RestAttachmentRequests => Interlocked.Read(ref _restAttachmentRequests);

    public void Record(RequestRecord record)
    {
        _records.Enqueue(record);
        Interlocked.Increment(ref _totalRequests);

        if (record.IsError)
        {
            Interlocked.Increment(ref _totalErrors);
        }

        IncrementMethodCounter(record.Method, record.Endpoint);
        RecordLatency(record.LatencyMs);
    }

    private void IncrementMethodCounter(string method, string endpoint)
    {
        if (string.Equals(method, "GraphQL", StringComparison.Ordinal))
        {
            Interlocked.Increment(ref _graphQLRequests);
        }
        else if (string.Equals(method, "REST", StringComparison.Ordinal)
            && endpoint.StartsWith("sync/", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _restSyncRequests);
        }
        else if (string.Equals(method, "REST", StringComparison.Ordinal)
            && endpoint.StartsWith("attachments/", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _restAttachmentRequests);
        }
    }

    private void RecordLatency(long latencyMs)
    {
        var latencyMicroseconds = latencyMs * 1000L;
        var clamped = Math.Max(1L, Math.Min(60_000_000L, latencyMicroseconds));

        lock (_histogramLock)
        {
            _latencyHistogram.RecordValue(clamped);
        }
    }

    public long GetLatencyPercentile(double percentile)
    {
        lock (_histogramLock)
        {
            return _latencyHistogram.GetValueAtPercentile(percentile) / 1000L;
        }
    }

    public IReadOnlyList<RequestRecord> GetRecords() => _records.ToArray();

    public double GetRequestsPerSecond()
    {
        var elapsed = (DateTime.UtcNow - _startTime).TotalSeconds;
        return elapsed <= 0 ? 0 : Interlocked.Read(ref _totalRequests) / elapsed;
    }
}
