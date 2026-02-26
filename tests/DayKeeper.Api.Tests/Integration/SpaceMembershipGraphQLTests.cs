using System.Net;
using System.Net.Http.Json;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public class SpaceMembershipGraphQLTests
{
    private readonly HttpClient _client;

    public SpaceMembershipGraphQLTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SpaceMemberships_Query_ReturnsConnectionType()
    {
        // Arrange
        var query = new
        {
            query = """
                {
                    spaceMemberships {
                        edges {
                            cursor
                            node {
                                role
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
        content.Should().Contain("\"spaceMemberships\"");
        content.Should().Contain("\"pageInfo\"");
        content.Should().Contain("\"totalCount\"");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task AddSpaceMember_Mutation_ReturnsMembership()
    {
        // Arrange
        var (tenantId, ownerId, spaceId) = await CreateTenantUserSpaceAsync();
        var memberId = await CreateUserAsync(tenantId);

        var query = new
        {
            query = $$"""
                mutation {
                    addSpaceMember(input: {
                        spaceId: "{{spaceId}}"
                        userId: "{{memberId}}"
                        role: EDITOR
                    }) {
                        spaceMembership {
                            role
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
        content.Should().Contain("\"role\":\"EDITOR\"");
        content.Should().NotContain("EntityNotFoundError");
        content.Should().NotContain("DuplicateMembershipError");
    }

    [Fact]
    public async Task AddSpaceMember_Mutation_Duplicate_ReturnsError()
    {
        // Arrange — space creator is already an Owner member
        var (tenantId, ownerId, spaceId) = await CreateTenantUserSpaceAsync();

        // Act — try to add the owner again
        var query = new
        {
            query = $$"""
                mutation {
                    addSpaceMember(input: {
                        spaceId: "{{spaceId}}"
                        userId: "{{ownerId}}"
                        role: VIEWER
                    }) {
                        spaceMembership { role }
                        errors {
                            __typename
                            ... on DuplicateMembershipError {
                                message
                            }
                        }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("DuplicateMembershipError");
    }

    [Fact]
    public async Task UpdateSpaceMemberRole_Mutation_ReturnsMembership()
    {
        // Arrange
        var (tenantId, ownerId, spaceId) = await CreateTenantUserSpaceAsync();
        var memberId = await CreateUserAsync(tenantId);
        await AddMemberAsync(spaceId, memberId, "VIEWER");

        // Act — promote to EDITOR
        var query = new
        {
            query = $$"""
                mutation {
                    updateSpaceMemberRole(input: {
                        spaceId: "{{spaceId}}"
                        userId: "{{memberId}}"
                        newRole: EDITOR
                    }) {
                        spaceMembership {
                            role
                        }
                        errors { __typename }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"role\":\"EDITOR\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task RemoveSpaceMember_Mutation_ReturnsTrue()
    {
        // Arrange
        var (tenantId, ownerId, spaceId) = await CreateTenantUserSpaceAsync();
        var memberId = await CreateUserAsync(tenantId);
        await AddMemberAsync(spaceId, memberId, "EDITOR");

        // Act
        var query = new
        {
            query = $$"""
                mutation {
                    removeSpaceMember(input: {
                        spaceId: "{{spaceId}}"
                        userId: "{{memberId}}"
                    }) {
                        boolean
                        errors { __typename }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("true");
        content.Should().NotContain("BusinessRuleViolationError");
    }

    [Fact]
    public async Task RemoveSpaceMember_LastOwner_ReturnsError()
    {
        // Arrange — space has only one owner (the creator)
        var (tenantId, ownerId, spaceId) = await CreateTenantUserSpaceAsync();

        // Act — try to remove the only owner
        var query = new
        {
            query = $$"""
                mutation {
                    removeSpaceMember(input: {
                        spaceId: "{{spaceId}}"
                        userId: "{{ownerId}}"
                    }) {
                        boolean
                        errors {
                            __typename
                            ... on BusinessRuleViolationError {
                                message
                            }
                        }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("BusinessRuleViolationError");
    }

    private async Task<(string TenantId, string UserId, string SpaceId)> CreateTenantUserSpaceAsync()
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

        var userId = await CreateUserAsync(tenantId).ConfigureAwait(false);

        var spaceName = $"Space-{Guid.NewGuid():N}";
        var spaceMutation = new
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
        var spaceResponse = await _client.PostAsJsonAsync("/graphql", spaceMutation).ConfigureAwait(false);
        var spaceContent = await spaceResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        var spaceId = ExtractId(spaceContent);

        return (tenantId, userId, spaceId);
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

    private async Task AddMemberAsync(string spaceId, string userId, string role)
    {
        var mutation = new
        {
            query = $$"""
                mutation {
                    addSpaceMember(input: {
                        spaceId: "{{spaceId}}"
                        userId: "{{userId}}"
                        role: {{role}}
                    }) {
                        spaceMembership { role }
                        errors { __typename }
                    }
                }
                """
        };
        await _client.PostAsJsonAsync("/graphql", mutation).ConfigureAwait(false);
    }

    private static string ExtractId(string json)
    {
        var marker = "\"id\":\"";
        var start = json.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = json.IndexOf('"', start);
        return json[start..end];
    }
}
