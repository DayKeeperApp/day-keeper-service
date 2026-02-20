using System.Net;
using System.Net.Http.Json;

namespace DayKeeper.Api.Tests.Integration;

public class HelloWorldEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HelloWorldEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHelloWorld_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/helloworld");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHelloWorld_ShouldReturnExpectedMessage()
    {
        // Act
        var response = await _client.GetFromJsonAsync<HelloWorldTestResponse>("/api/helloworld");

        // Assert
        response.Should().NotBeNull();
        response!.Message.Should().Be("Hello from DayKeeper!");
        response.Version.Should().Be("1.0.0");
    }

    [Fact]
    public async Task LivenessProbe_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadinessProbe_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record HelloWorldTestResponse(string Message, DateTime Timestamp, string Version);
}
