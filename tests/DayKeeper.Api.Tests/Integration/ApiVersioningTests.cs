using System.Net;
using System.Net.Http.Json;
using DayKeeper.Application.DTOs.Sync;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public class ApiVersioningTests
{
    private readonly HttpClient _client;

    public ApiVersioningTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task VersionedRoute_ShouldReturnOk()
    {
        // Act — use the Sync pull endpoint as a versioned REST target
        var request = new SyncPullRequest(0, null, 10);
        var response = await _client.PostAsJsonAsync("/api/v1/sync/pull", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnversionedRoute_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.PostAsync("/api/sync/pull", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task VersionedRoute_ShouldReportSupportedVersions()
    {
        // Act
        var request = new SyncPullRequest(0, null, 10);
        var response = await _client.PostAsJsonAsync("/api/v1/sync/pull", request);

        // Assert
        response.Headers.Should().ContainKey("api-supported-versions");
        var versions = response.Headers.GetValues("api-supported-versions");
        versions.Should().Contain(v => v.Contains("1.0"));
    }
}
