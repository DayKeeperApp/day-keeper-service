using System.Net;
using System.Net.Http.Json;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public sealed class ErrorFilterGraphQLTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ErrorFilterGraphQLTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SpaceById_WhenServiceThrowsUnexpectedException_ReturnsInternalErrorCode()
    {
        // Arrange: override ISpaceService so GetSpaceAsync throws an unexpected exception
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var faultingService = Substitute.For<ISpaceService>();
                faultingService
                    .When(s => s.GetSpaceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()))
                    .Do(_ => throw new InvalidOperationException("Internal state is corrupt"));

                services.AddScoped<ISpaceService>(_ => faultingService);
            });
        }).CreateClient();

        var query = new
        {
            query = $$"""
                {
                    spaceById(id: "{{Guid.NewGuid()}}") {
                        id
                        name
                    }
                }
                """,
        };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert: HC returns 200 even for errors; error appears in the errors array.
        // The test host runs in Development mode, so the detail extension is included.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("INTERNAL_ERROR");
        content.Should().Contain("An unexpected error occurred.");
        content.Should().Contain("exceptionType");
        content.Should().Contain("System.InvalidOperationException");
    }

    [Fact]
    public async Task SpaceById_WhenServiceThrowsEntityNotFoundException_ReturnsNotFoundCode()
    {
        // Arrange: override ISpaceService so GetSpaceAsync throws EntityNotFoundException
        var spaceId = Guid.NewGuid();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var faultingService = Substitute.For<ISpaceService>();
                faultingService
                    .When(s => s.GetSpaceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()))
                    .Do(_ => throw new Application.Exceptions.EntityNotFoundException("Space", spaceId));

                services.AddScoped<ISpaceService>(_ => faultingService);
            });
        }).CreateClient();

        var query = new
        {
            query = $$"""
                {
                    spaceById(id: "{{spaceId}}") {
                        id
                        name
                    }
                }
                """,
        };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("NOT_FOUND");
        content.Should().Contain("Space");
    }
}
