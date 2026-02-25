using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public class GraphQLEndpointTests
{
    private readonly HttpClient _client;

    public GraphQLEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GraphQL_Endpoint_ShouldReturnOk()
    {
        // Arrange
        var query = new { query = "{ serviceStatus }" };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GraphQL_ServiceStatus_ShouldReturnExpectedMessage()
    {
        // Arrange
        var query = new { query = "{ serviceStatus }" };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadFromJsonAsync<GraphQLResponse>();

        // Assert
        content.Should().NotBeNull();
        content!.Data.Should().NotBeNull();
        content.Data!.ServiceStatus.Should().Be("DayKeeper GraphQL API is running");
    }

    [Fact]
    public async Task GraphQL_IntrospectionQuery_ShouldReturnSchema()
    {
        // Arrange
        var query = new { query = "{ __schema { queryType { name } } }" };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record GraphQLResponse(
        [property: JsonPropertyName("data")] GraphQLData? Data);

    private sealed record GraphQLData(
        [property: JsonPropertyName("serviceStatus")] string? ServiceStatus);
}
