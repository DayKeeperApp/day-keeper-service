using System.Net;
using System.Net.Http.Json;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public class ValidationGraphQLTests
{
    private readonly HttpClient _client;

    public ValidationGraphQLTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // -------------------------------------------------------------------------
    // CreateTenant — name validation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateTenant_EmptyName_ReturnsInputValidationError()
    {
        // Arrange
        var slug = $"v-{Guid.NewGuid():N}";
        var mutation = new
        {
            query = $$"""
                mutation {
                    createTenant(input: { name: "", slug: "{{slug}}" }) {
                        tenant { id }
                        errors {
                            __typename
                            ... on InputValidationError {
                                message
                                errors { key value }
                            }
                        }
                    }
                }
                """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("InputValidationError");
    }

    // -------------------------------------------------------------------------
    // CreateTenant — slug format validation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateTenant_InvalidSlugFormat_ReturnsInputValidationError()
    {
        // Arrange — slug contains uppercase letters and a space, violating ^[a-z0-9]+(?:-[a-z0-9]+)*$
        var mutation = new
        {
            query = """
                mutation {
                    createTenant(input: { name: "Valid Name", slug: "INVALID SLUG!" }) {
                        tenant { id }
                        errors {
                            __typename
                            ... on InputValidationError {
                                message
                                errors { key value }
                            }
                        }
                    }
                }
                """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("InputValidationError");
    }

    // -------------------------------------------------------------------------
    // CreateUser — email validation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateUser_InvalidEmail_ReturnsInputValidationError()
    {
        // Arrange — create a real tenant so TenantId passes NotEmpty, isolating the email failure
        var tenantId = await CreateTenantAsync();
        var mutation = new
        {
            query = $$"""
                mutation {
                    createUser(input: {
                        tenantId: "{{tenantId}}"
                        displayName: "Test User"
                        email: "not-an-email"
                        timezone: "UTC"
                        weekStart: MONDAY
                    }) {
                        user { id }
                        errors {
                            __typename
                            ... on InputValidationError {
                                message
                                errors { key value }
                            }
                        }
                    }
                }
                """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("InputValidationError");
    }

    // -------------------------------------------------------------------------
    // CreateUser — timezone validation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateUser_InvalidTimezone_ReturnsInputValidationError()
    {
        // Arrange — create a real tenant so TenantId passes NotEmpty, isolating the timezone failure
        var tenantId = await CreateTenantAsync();
        var mutation = new
        {
            query = $$"""
                mutation {
                    createUser(input: {
                        tenantId: "{{tenantId}}"
                        displayName: "Test User"
                        email: "valid@example.com"
                        timezone: "Not/A/Real/Timezone"
                        weekStart: MONDAY
                    }) {
                        user { id }
                        errors {
                            __typename
                            ... on InputValidationError {
                                message
                                errors { key value }
                            }
                        }
                    }
                }
                """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("InputValidationError");
    }

    // -------------------------------------------------------------------------
    // CreateSpace — name validation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateSpace_EmptyName_ReturnsInputValidationError()
    {
        // Arrange — create a real tenant and user so those IDs pass NotEmpty
        var tenantId = await CreateTenantAsync();
        var userId = await CreateUserAsync(tenantId);
        var mutation = new
        {
            query = $$"""
                mutation {
                    createSpace(input: {
                        tenantId: "{{tenantId}}"
                        name: ""
                        spaceType: SHARED
                        createdByUserId: "{{userId}}"
                    }) {
                        space { id }
                        errors {
                            __typename
                            ... on InputValidationError {
                                message
                                errors { key value }
                            }
                        }
                    }
                }
                """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("InputValidationError");
    }

    // -------------------------------------------------------------------------
    // AddSpaceMember — empty GUID validation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddSpaceMember_EmptySpaceId_ReturnsInputValidationError()
    {
        // Arrange — Guid.Empty ("00000000-...") passes GraphQL type parsing but fails NotEmpty()
        var emptyGuid = Guid.Empty;
        var tenantId = await CreateTenantAsync();
        var userId = await CreateUserAsync(tenantId);
        var mutation = new
        {
            query = $$"""
                mutation {
                    addSpaceMember(input: {
                        spaceId: "{{emptyGuid}}"
                        userId: "{{userId}}"
                        role: VIEWER
                    }) {
                        spaceMembership { role }
                        errors {
                            __typename
                            ... on InputValidationError {
                                message
                                errors { key value }
                            }
                        }
                    }
                }
                """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("InputValidationError");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

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
