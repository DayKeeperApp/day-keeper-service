using System.Net;
using System.Net.Http.Json;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public class SpacePaginationTests
{
    private readonly HttpClient _client;

    public SpacePaginationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Spaces_Query_ReturnsConnectionType()
    {
        // Arrange
        var query = new
        {
            query = """
                {
                    spaces {
                        edges {
                            cursor
                            node {
                                name
                                spaceType
                            }
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                        totalCount
                    }
                }
                """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"spaces\"");
        content.Should().Contain("\"pageInfo\"");
        content.Should().Contain("\"totalCount\"");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task Spaces_Query_SupportsFirstArgument()
    {
        // Arrange
        var query = new
        {
            query = """
                {
                    spaces(first: 5) {
                        nodes {
                            name
                        }
                        pageInfo {
                            hasNextPage
                        }
                        totalCount
                    }
                }
                """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task Spaces_IntrospectionQuery_ExposesConnectionType()
    {
        // Arrange
        var query = new
        {
            query = """
                {
                    __type(name: "SpacesConnection") {
                        name
                        fields {
                            name
                        }
                    }
                }
                """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("SpacesConnection");
        content.Should().Contain("edges");
        content.Should().Contain("pageInfo");
    }
}
