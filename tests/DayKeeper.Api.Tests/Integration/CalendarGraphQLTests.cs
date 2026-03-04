using System.Net;
using System.Net.Http.Json;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public class CalendarGraphQLTests
{
    private readonly HttpClient _client;

    public CalendarGraphQLTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Queries ──────────────────────────────────────────────────────

    [Fact]
    public async Task Calendars_Query_ReturnsConnectionType()
    {
        var query = new
        {
            query = """
                {
                    calendars {
                        edges {
                            cursor
                            node {
                                id
                                name
                                color
                                isDefault
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

        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"calendars\"");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task CalendarById_Query_ReturnsNullForNonExistent()
    {
        var id = Guid.NewGuid();
        var query = new
        {
            query = $$"""
                {
                    calendarById(id: "{{id}}") {
                        id
                        name
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"calendarById\":null");
        content.Should().NotContain("\"errors\"");
    }

    // ── Create Mutation ──────────────────────────────────────────────

    [Fact]
    public async Task CreateCalendar_Mutation_ReturnsCalendar()
    {
        var spaceId = await CreateSpaceAsync();
        var calendarName = $"Cal-{Guid.NewGuid():N}";

        var mutation = new
        {
            query = $$"""
                mutation {
                    createCalendar(input: {
                        spaceId: "{{spaceId}}"
                        name: "{{calendarName}}"
                        color: "#4A90D9"
                        isDefault: false
                    }) {
                        calendar {
                            id
                            name
                            color
                            isDefault
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain($"\"name\":\"{calendarName}\"");
        content.Should().Contain("\"color\":\"#4A90D9\"");
        content.Should().NotContain("EntityNotFoundError");
        content.Should().NotContain("DuplicateCalendarNameError");
    }

    [Fact]
    public async Task CreateCalendar_Mutation_DuplicateName_ReturnsError()
    {
        var spaceId = await CreateSpaceAsync();
        var calendarName = $"Dup-{Guid.NewGuid():N}";

        var firstCreate = new
        {
            query = $$"""
                mutation {
                    createCalendar(input: {
                        spaceId: "{{spaceId}}"
                        name: "{{calendarName}}"
                        color: "#4A90D9"
                        isDefault: false
                    }) {
                        calendar { id }
                        errors { __typename }
                    }
                }
                """
        };
        await _client.PostAsJsonAsync("/graphql", firstCreate);

        // Duplicate
        var duplicate = new
        {
            query = $$"""
                mutation {
                    createCalendar(input: {
                        spaceId: "{{spaceId}}"
                        name: "{{calendarName}}"
                        color: "#FF0000"
                        isDefault: false
                    }) {
                        calendar { id }
                        errors {
                            __typename
                            ... on DuplicateCalendarNameError {
                                message
                            }
                        }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", duplicate);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("DuplicateCalendarNameError");
    }

    // ── Update Mutation ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateCalendar_Mutation_ReturnsUpdatedCalendar()
    {
        var spaceId = await CreateSpaceAsync();
        var calendarId = await CreateCalendarAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    updateCalendar(input: { id: "{{calendarId}}", name: "Updated Calendar" }) {
                        calendar {
                            id
                            name
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"name\":\"Updated Calendar\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task UpdateCalendar_Mutation_NotFound_ReturnsError()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    updateCalendar(input: { id: "{{id}}", name: "Nope" }) {
                        calendar { id }
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

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("EntityNotFoundError");
    }

    // ── Delete Mutation ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteCalendar_Mutation_ReturnsTrue()
    {
        var spaceId = await CreateSpaceAsync();
        var calendarId = await CreateCalendarAsync(spaceId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    deleteCalendar(input: { id: "{{calendarId}}" }) {
                        boolean
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("true");
    }

    [Fact]
    public async Task DeleteCalendar_Mutation_WhenNotFound_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    deleteCalendar(input: { id: "{{id}}" }) {
                        boolean
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("false");
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private async Task<string> CreateSpaceAsync()
    {
        var (tenantId, userId) = await CreateTenantAndUserAsync().ConfigureAwait(false);
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

    private static string ExtractId(string json)
    {
        var marker = "\"id\":\"";
        var start = json.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = json.IndexOf('"', start);
        return json[start..end];
    }
}
