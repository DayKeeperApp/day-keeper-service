using System.Net;
using System.Net.Http.Json;

namespace DayKeeper.Api.Tests.Integration;

/// <summary>
/// Regression tests verifying that *ById queries correctly resolve
/// navigation properties via HotChocolate projections.
/// </summary>
[Collection("Integration")]
public class NavigationPropertyTests
{
    private readonly HttpClient _client;

    public NavigationPropertyTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProjectById_WhenExists_ReturnsSpaceNavigation()
    {
        // Arrange: create tenant → space → project
        var (tenantId, userId) = await CreateTenantAndUserAsync();
        var spaceId = await CreateSpaceAsync(tenantId, userId);
        var projectId = await CreateProjectAsync(spaceId);

        var query = new
        {
            query = $$"""
                {
                    projectById(id: "{{projectId}}") {
                        id
                        name
                        space {
                            id
                            name
                        }
                    }
                }
                """
        };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        // Assert: navigation property is resolved, not null
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"space\":{");
        content.Should().Contain($"\"id\":\"{spaceId}\"");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task CalendarById_WhenExists_ReturnsSpaceNavigation()
    {
        // Arrange: create tenant → space → calendar
        var (tenantId, userId) = await CreateTenantAndUserAsync();
        var spaceId = await CreateSpaceAsync(tenantId, userId);
        var calendarId = await CreateCalendarAsync(spaceId);

        var query = new
        {
            query = $$"""
                {
                    calendarById(id: "{{calendarId}}") {
                        id
                        name
                        space {
                            id
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
        content.Should().Contain("\"space\":{");
        content.Should().Contain($"\"id\":\"{spaceId}\"");
        content.Should().NotContain("\"errors\"");
    }

    // ── Helpers ──────────────────────────────────────────────────────

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

    private async Task<string> CreateProjectAsync(string spaceId)
    {
        var name = $"Proj-{Guid.NewGuid():N}";
        var mutation = new
        {
            query = $$"""
                mutation {
                    createProject(input: {
                        spaceId: "{{spaceId}}"
                        name: "{{name}}"
                    }) {
                        project { id }
                        errors { __typename }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", mutation).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ExtractId(content);
    }

    private async Task<string> CreateCalendarAsync(string spaceId)
    {
        var name = $"Cal-{Guid.NewGuid():N}";
        var mutation = new
        {
            query = $$"""
                mutation {
                    createCalendar(input: {
                        spaceId: "{{spaceId}}"
                        name: "{{name}}"
                        color: "#4A90D9"
                        isDefault: false
                    }) {
                        calendar { id }
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
