namespace DayKeeper.Api.GraphQL;

/// <summary>
/// Root query type for the DayKeeper GraphQL API.
/// Domain-specific queries will be added in subsequent tasks.
/// </summary>
public class Query
{
    /// <summary>
    /// Returns the service status. Useful for verifying the GraphQL endpoint is operational.
    /// </summary>
    public string GetServiceStatus() => "DayKeeper GraphQL API is running";
}
