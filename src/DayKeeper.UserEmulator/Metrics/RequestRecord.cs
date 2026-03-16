namespace DayKeeper.UserEmulator.Metrics;

public sealed record RequestRecord(
    string Endpoint,
    string Method,
    int StatusCode,
    long LatencyMs,
    string PersonaName,
    string ArchetypeName,
    bool IsError,
    DateTime Timestamp);
