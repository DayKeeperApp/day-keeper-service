using System.Net;
using System.Net.Http.Json;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public class SpaceMutationTests
{
    private readonly HttpClient _client;

    public SpaceMutationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SpaceById_Query_ReturnsNullForNonExistent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var query = new
        {
            query = $$"""
                {
                    spaceById(id: "{{id}}") {
                        id
                        name
                    }
                }
                """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"spaceById\":null");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task CreateSpace_Mutation_ReturnsSpace()
    {
        // Arrange
        var (tenantId, userId) = await CreateTenantAndUserAsync();
        var spaceName = $"Space-{Guid.NewGuid():N}";

        var query = new
        {
            query = $$"""
                mutation {
                    createSpace(input: {
                        tenantId: "{{tenantId}}"
                        name: "{{spaceName}}"
                        spaceType: PERSONAL
                        createdByUserId: "{{userId}}"
                    }) {
                        space {
                            id
                            name
                            spaceType
                        }
                        errors { __typename }
                    }
                }
                """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain($"\"name\":\"{spaceName}\"");
        content.Should().Contain("\"spaceType\":\"PERSONAL\"");
        content.Should().NotContain("EntityNotFoundError");
        content.Should().NotContain("DuplicateSpaceNameError");
    }

    [Fact]
    public async Task CreateSpace_Mutation_DuplicateName_ReturnsError()
    {
        // Arrange
        var (tenantId, userId) = await CreateTenantAndUserAsync();
        var spaceName = $"Dup-{Guid.NewGuid():N}";

        var firstCreate = new
        {
            query = $$"""
                mutation {
                    createSpace(input: {
                        tenantId: "{{tenantId}}"
                        name: "{{spaceName}}"
                        spaceType: SHARED
                        createdByUserId: "{{userId}}"
                    }) {
                        space { id }
                        errors { __typename }
                    }
                }
                """
        };
        await _client.PostAsJsonAsync("/graphql", firstCreate);

        // Act — duplicate
        var duplicate = new
        {
            query = $$"""
                mutation {
                    createSpace(input: {
                        tenantId: "{{tenantId}}"
                        name: "{{spaceName}}"
                        spaceType: SHARED
                        createdByUserId: "{{userId}}"
                    }) {
                        space { id }
                        errors {
                            __typename
                            ... on DuplicateSpaceNameError {
                                message
                            }
                        }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", duplicate);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("DuplicateSpaceNameError");
    }

    [Fact]
    public async Task UpdateSpace_Mutation_ReturnsSpace()
    {
        // Arrange
        var (tenantId, userId) = await CreateTenantAndUserAsync();
        var spaceId = await CreateSpaceAsync(tenantId, userId);

        var query = new
        {
            query = $$"""
                mutation {
                    updateSpace(input: { id: "{{spaceId}}", name: "Updated Space" }) {
                        space {
                            id
                            name
                        }
                        errors { __typename }
                    }
                }
                """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"name\":\"Updated Space\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task DeleteSpace_Mutation_ReturnsTrue()
    {
        // Arrange
        var (tenantId, userId) = await CreateTenantAndUserAsync();
        var spaceId = await CreateSpaceAsync(tenantId, userId);

        var query = new
        {
            query = $$"""
                mutation {
                    deleteSpace(input: { id: "{{spaceId}}" }) {
                        boolean
                    }
                }
                """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("true");
    }

    private async Task<(string TenantId, string UserId)> CreateTenantAndUserAsync()
    {
        var slug = $"t-{Guid.NewGuid():N}";
        var tenantMutation = new
        {
            query = $$"""
                mutation {
                    createTenant(input: { name: "Tenant", slug: "{{slug}}" }) {
                        tenant { id }
                        errors { __typename }
                    }
                }
                """
        };
        var tenantResponse = await _client.PostAsJsonAsync("/graphql", tenantMutation).ConfigureAwait(false);
        var tenantContent = await tenantResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        var tenantId = ExtractId(tenantContent);

        var email = $"u-{Guid.NewGuid():N}@example.com";
        var userMutation = new
        {
            query = $$"""
                mutation {
                    createUser(input: {
                        tenantId: "{{tenantId}}"
                        displayName: "User"
                        email: "{{email}}"
                        timezone: "UTC"
                        weekStart: SUNDAY
                    }) {
                        user { id }
                        errors { __typename }
                    }
                }
                """
        };
        var userResponse = await _client.PostAsJsonAsync("/graphql", userMutation).ConfigureAwait(false);
        var userContent = await userResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        var userId = ExtractId(userContent);

        return (tenantId, userId);
    }

    private async Task<string> CreateSpaceAsync(string tenantId, string userId)
    {
        var name = $"Space-{Guid.NewGuid():N}";
        var mutation = new
        {
            query = $$"""
                mutation {
                    createSpace(input: {
                        tenantId: "{{tenantId}}"
                        name: "{{name}}"
                        spaceType: PERSONAL
                        createdByUserId: "{{userId}}"
                    }) {
                        space { id }
                        errors { __typename }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", mutation).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ExtractId(content);
    }

    private static string ExtractId(string json)
    {
        var marker = "\"id\":\"";
        var start = json.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = json.IndexOf('"', start);
        return json[start..end];
    }
}
