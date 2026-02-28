using System.Net;
using System.Net.Http.Json;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public class UserGraphQLTests
{
    private readonly HttpClient _client;

    public UserGraphQLTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Users_Query_ReturnsConnectionType()
    {
        // Arrange
        var query = new
        {
            query = """
                {
                    users {
                        edges {
                            cursor
                            node {
                                displayName
                                email
                            }
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
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
        content.Should().Contain("\"users\"");
        content.Should().Contain("\"pageInfo\"");
        content.Should().Contain("\"totalCount\"");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task UserById_Query_ReturnsNullForNonExistent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var query = new
        {
            query = $$"""
                {
                    userById(id: "{{id}}") {
                        id
                        displayName
                    }
                }
                """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"userById\":null");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task CreateUser_Mutation_ReturnsUser()
    {
        // Arrange — create tenant first
        var tenantId = await CreateTenantAsync();
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var query = new
        {
            query = $$"""
                mutation {
                    createUser(input: {
                        tenantId: "{{tenantId}}"
                        displayName: "Test User"
                        email: "{{email}}"
                        timezone: "America/New_York"
                        weekStart: SUNDAY
                    }) {
                        user {
                            id
                            displayName
                            email
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
        content.Should().Contain("\"displayName\":\"Test User\"");
        content.Should().NotContain("DuplicateEmailError");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task CreateUser_Mutation_DuplicateEmail_ReturnsError()
    {
        // Arrange
        var tenantId = await CreateTenantAsync();
        var email = $"dup-{Guid.NewGuid():N}@example.com";

        var firstCreate = new
        {
            query = $$"""
                mutation {
                    createUser(input: {
                        tenantId: "{{tenantId}}"
                        displayName: "First"
                        email: "{{email}}"
                        timezone: "UTC"
                        weekStart: MONDAY
                    }) {
                        user { id }
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
                    createUser(input: {
                        tenantId: "{{tenantId}}"
                        displayName: "Second"
                        email: "{{email}}"
                        timezone: "UTC"
                        weekStart: MONDAY
                    }) {
                        user { id }
                        errors {
                            __typename
                            ... on DuplicateEmailError {
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
        content.Should().Contain("DuplicateEmailError");
    }

    [Fact]
    public async Task CreateUser_Mutation_TenantNotFound_ReturnsError()
    {
        // Arrange
        var fakeTenantId = Guid.NewGuid();
        var query = new
        {
            query = $$"""
                mutation {
                    createUser(input: {
                        tenantId: "{{fakeTenantId}}"
                        displayName: "Orphan"
                        email: "orphan@example.com"
                        timezone: "UTC"
                        weekStart: SUNDAY
                    }) {
                        user { id }
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
    public async Task DeleteUser_Mutation_ReturnsTrue()
    {
        // Arrange
        var tenantId = await CreateTenantAsync();
        var userId = await CreateUserAsync(tenantId);

        var query = new
        {
            query = $$"""
                mutation {
                    deleteUser(input: { id: "{{userId}}" }) {
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

    [Fact]
    public async Task UpdateUser_Mutation_ReturnsUser()
    {
        // Arrange
        var tenantId = await CreateTenantAsync();
        var userId = await CreateUserAsync(tenantId);

        var query = new
        {
            query = $$"""
                mutation {
                    updateUser(input: {
                        id: "{{userId}}"
                        displayName: "Updated Name"
                    }) {
                        user {
                            id
                            displayName
                            email
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
        content.Should().Contain("\"displayName\":\"Updated Name\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task UpdateUser_Mutation_NotFound_ReturnsError()
    {
        // Arrange
        var id = Guid.NewGuid();
        var query = new
        {
            query = $$"""
                mutation {
                    updateUser(input: { id: "{{id}}", displayName: "Nope" }) {
                        user { id }
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
    public async Task UserByEmail_Query_ReturnsUser()
    {
        // Arrange
        var tenantId = await CreateTenantAsync();
        var email = $"byemail-{Guid.NewGuid():N}@example.com";

        var createMutation = new
        {
            query = $$"""
                mutation {
                    createUser(input: {
                        tenantId: "{{tenantId}}"
                        displayName: "ByEmail User"
                        email: "{{email}}"
                        timezone: "UTC"
                        weekStart: MONDAY
                    }) {
                        user { id }
                        errors { __typename }
                    }
                }
                """
        };
        await _client.PostAsJsonAsync("/graphql", createMutation);

        // Act
        var query = new
        {
            query = $$"""
                {
                    userByEmail(tenantId: "{{tenantId}}", email: "{{email}}") {
                        id
                        displayName
                        email
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"displayName\":\"ByEmail User\"");
        content.Should().Contain($"\"email\":\"{email}\"");
        content.Should().NotContain("\"errors\"");
    }

    private async Task<string> CreateTenantAsync()
    {
        var slug = $"t-{Guid.NewGuid():N}";
        var mutation = new
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
        var response = await _client.PostAsJsonAsync("/graphql", mutation).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ExtractId(content);
    }

    private async Task<string> CreateUserAsync(string tenantId)
    {
        var email = $"u-{Guid.NewGuid():N}@example.com";
        var mutation = new
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
