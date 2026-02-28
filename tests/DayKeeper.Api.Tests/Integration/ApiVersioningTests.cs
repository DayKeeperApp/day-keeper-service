using System.Net;

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
        // Act
        var response = await _client.GetAsync("/api/v1/helloworld");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnversionedRoute_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/helloworld");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task VersionedRoute_ShouldReportSupportedVersions()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/helloworld");

        // Assert
        response.Headers.Should().ContainKey("api-supported-versions");
        var versions = response.Headers.GetValues("api-supported-versions");
        versions.Should().Contain(v => v.Contains("1.0"));
    }
}
