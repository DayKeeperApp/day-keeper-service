using System.Net;
using System.Net.Http.Json;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public class TenantGraphQLTests
{
    private readonly HttpClient _client;

    public TenantGraphQLTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Tenants_Query_ReturnsConnectionType()
    {
        // Arrange
        var query = new
        {
            query = """
                {
                    tenants {
                        edges {
                            cursor
                            node {
                                name
                                slug
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
        content.Should().Contain("\"tenants\"");
        content.Should().Contain("\"pageInfo\"");
        content.Should().Contain("\"totalCount\"");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task TenantById_Query_ReturnsNullForNonExistent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var query = new
        {
            query = $$"""
                {
                    tenantById(id: "{{id}}") {
                        id
                        name
                        slug
                    }
                }
                """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"tenantById\":null");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task CreateTenant_Mutation_ReturnsTenant()
    {
        // Arrange
        var slug = $"test-{Guid.NewGuid():N}";
        var query = new
        {
            query = $$"""
                mutation {
                    createTenant(input: { name: "Test Org", slug: "{{slug}}" }) {
                        tenant {
                            id
                            name
                            slug
                        }
                        errors {
                            __typename
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
        content.Should().Contain("\"name\":\"Test Org\"");
        content.Should().Contain($"\"slug\":\"{slug}\"");
        content.Should().NotContain("DuplicateSlugError");
    }

    [Fact]
    public async Task CreateTenant_Mutation_DuplicateSlug_ReturnsError()
    {
        // Arrange — create first tenant
        var slug = $"dup-{Guid.NewGuid():N}";
        var create = new
        {
            query = $$"""
                mutation {
                    createTenant(input: { name: "First", slug: "{{slug}}" }) {
                        tenant { id }
                        errors { __typename }
                    }
                }
                """
        };
        await _client.PostAsJsonAsync("/graphql", create);

        // Act — create second with same slug
        var duplicate = new
        {
            query = $$"""
                mutation {
                    createTenant(input: { name: "Second", slug: "{{slug}}" }) {
                        tenant { id }
                        errors {
                            __typename
                            ... on DuplicateSlugError {
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
        content.Should().Contain("DuplicateSlugError");
    }

    [Fact]
    public async Task UpdateTenant_Mutation_ReturnsTenant()
    {
        // Arrange — create tenant
        var slug = $"upd-{Guid.NewGuid():N}";
        var create = new
        {
            query = $$"""
                mutation {
                    createTenant(input: { name: "Original", slug: "{{slug}}" }) {
                        tenant { id }
                        errors { __typename }
                    }
                }
                """
        };
        var createResponse = await _client.PostAsJsonAsync("/graphql", create);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var tenantId = ExtractId(createContent);

        // Act — update
        var update = new
        {
            query = $$"""
                mutation {
                    updateTenant(input: { id: "{{tenantId}}", name: "Updated" }) {
                        tenant {
                            id
                            name
                        }
                        errors { __typename }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", update);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"name\":\"Updated\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task UpdateTenant_Mutation_NotFound_ReturnsError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var query = new
        {
            query = $$"""
                mutation {
                    updateTenant(input: { id: "{{id}}", name: "Nope" }) {
                        tenant { id }
                        errors {
                            __typename
                            ... on EntityNotFoundError {
                                message
                            }
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
        content.Should().Contain("EntityNotFoundError");
    }

    [Fact]
    public async Task DeleteTenant_Mutation_ReturnsTrue()
    {
        // Arrange — create tenant
        var slug = $"del-{Guid.NewGuid():N}";
        var create = new
        {
            query = $$"""
                mutation {
                    createTenant(input: { name: "ToDelete", slug: "{{slug}}" }) {
                        tenant { id }
                        errors { __typename }
                    }
                }
                """
        };
        var createResponse = await _client.PostAsJsonAsync("/graphql", create);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var tenantId = ExtractId(createContent);

        // Act
        var delete = new
        {
            query = $$"""
                mutation {
                    deleteTenant(input: { id: "{{tenantId}}" }) {
                        boolean
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", delete);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("true");
    }

    [Fact]
    public async Task TenantBySlug_Query_ReturnsTenantWhenExists()
    {
        // Arrange
        var slug = $"slug-{Guid.NewGuid():N}";
        var create = new
        {
            query = $$"""
                mutation {
                    createTenant(input: { name: "SlugTest", slug: "{{slug}}" }) {
                        tenant { id }
                        errors { __typename }
                    }
                }
                """
        };
        await _client.PostAsJsonAsync("/graphql", create);

        // Act
        var query = new
        {
            query = $$"""
                {
                    tenantBySlug(slug: "{{slug}}") {
                        id
                        name
                        slug
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"name\":\"SlugTest\"");
        content.Should().Contain($"\"slug\":\"{slug}\"");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task DeleteTenant_Mutation_WhenNotFound_ReturnsFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        var query = new
        {
            query = $$"""
                mutation {
                    deleteTenant(input: { id: "{{id}}" }) {
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
        content.Should().Contain("false");
    }

    private static string ExtractId(string json)
    {
        // Simple extraction of "id":"<value>" from JSON response
        var marker = "\"id\":\"";
        var start = json.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = json.IndexOf('"', start);
        return json[start..end];
    }
}
