using System.Net;
using System.Net.Http.Json;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public class CalendarEventGraphQLTests
{
    private readonly HttpClient _client;

    public CalendarEventGraphQLTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Queries ──────────────────────────────────────────────────────

    [Fact]
    public async Task CalendarEvents_Query_ReturnsConnectionType()
    {
        var query = new
        {
            query = """
                {
                    calendarEvents {
                        edges {
                            cursor
                            node {
                                id
                                title
                                isAllDay
                                timezone
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
        content.Should().Contain("\"calendarEvents\"");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task CalendarEventById_Query_ReturnsNullForNonExistent()
    {
        var id = Guid.NewGuid();
        var query = new
        {
            query = $$"""
                {
                    calendarEventById(id: "{{id}}") {
                        id
                        title
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"calendarEventById\":null");
        content.Should().NotContain("\"errors\"");
    }

    // ── Create Mutation ──────────────────────────────────────────────

    [Fact]
    public async Task CreateCalendarEvent_Mutation_ReturnsEvent()
    {
        var calendarId = await CreateCalendarAsync();
        var title = $"Event-{Guid.NewGuid():N}";

        var mutation = new
        {
            query = $$"""
                mutation {
                    createCalendarEvent(input: {
                        calendarId: "{{calendarId}}"
                        title: "{{title}}"
                        isAllDay: false
                        startAt: "2026-03-01T15:00:00Z"
                        endAt: "2026-03-01T16:00:00Z"
                        timezone: "America/Chicago"
                    }) {
                        calendarEvent {
                            id
                            title
                            isAllDay
                            timezone
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain($"\"title\":\"{title}\"");
        content.Should().Contain("\"timezone\":\"America/Chicago\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task CreateCalendarEvent_Mutation_CalendarNotFound_ReturnsError()
    {
        var fakeCalendarId = Guid.NewGuid();

        var mutation = new
        {
            query = $$"""
                mutation {
                    createCalendarEvent(input: {
                        calendarId: "{{fakeCalendarId}}"
                        title: "Orphan Event"
                        isAllDay: false
                        startAt: "2026-03-01T15:00:00Z"
                        endAt: "2026-03-01T16:00:00Z"
                        timezone: "UTC"
                    }) {
                        calendarEvent { id }
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

    // ── Update Mutation ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateCalendarEvent_Mutation_ReturnsUpdatedEvent()
    {
        var calendarId = await CreateCalendarAsync();
        var eventId = await CreateCalendarEventAsync(calendarId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    updateCalendarEvent(input: {
                        id: "{{eventId}}"
                        title: "Updated Event"
                        location: "Room B"
                    }) {
                        calendarEvent {
                            id
                            title
                            location
                        }
                        errors { __typename }
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", mutation);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"title\":\"Updated Event\"");
        content.Should().Contain("\"location\":\"Room B\"");
        content.Should().NotContain("EntityNotFoundError");
    }

    [Fact]
    public async Task UpdateCalendarEvent_Mutation_NotFound_ReturnsError()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    updateCalendarEvent(input: { id: "{{id}}", title: "Nope" }) {
                        calendarEvent { id }
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
    public async Task DeleteCalendarEvent_Mutation_ReturnsTrue()
    {
        var calendarId = await CreateCalendarAsync();
        var eventId = await CreateCalendarEventAsync(calendarId);

        var mutation = new
        {
            query = $$"""
                mutation {
                    deleteCalendarEvent(input: { id: "{{eventId}}" }) {
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
    public async Task DeleteCalendarEvent_Mutation_WhenNotFound_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var mutation = new
        {
            query = $$"""
                mutation {
                    deleteCalendarEvent(input: { id: "{{id}}" }) {
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

    // ── EventsForRange Query ─────────────────────────────────────────

    [Fact]
    public async Task EventsForRange_Query_ReturnsExpandedOccurrences()
    {
        var calendarId = await CreateCalendarAsync();
        await CreateCalendarEventAsync(calendarId);

        var query = new
        {
            query = $$"""
                {
                    eventsForRange(
                        calendarIds: ["{{calendarId}}"]
                        rangeStart: "2026-03-01T00:00:00Z"
                        rangeEnd: "2026-03-31T00:00:00Z"
                        timezone: "UTC"
                    ) {
                        calendarEventId
                        title
                        startAt
                        endAt
                        isRecurring
                        isException
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", query);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"eventsForRange\"");
        content.Should().Contain("\"isRecurring\":false");
        content.Should().NotContain("\"errors\"");
    }

    [Fact]
    public async Task EventsForRange_Query_WithRecurringEvent_ExpandsCorrectly()
    {
        var calendarId = await CreateCalendarAsync();

        // Create a recurring event via GraphQL
        var mutation = new
        {
            query = $$"""
                mutation {
                    createCalendarEvent(input: {
                        calendarId: "{{calendarId}}"
                        title: "Daily Standup"
                        isAllDay: false
                        startAt: "2026-03-01T15:00:00Z"
                        endAt: "2026-03-01T15:30:00Z"
                        timezone: "UTC"
                        recurrenceRule: "FREQ=DAILY;COUNT=5"
                        recurrenceEndAt: "2026-03-05T15:00:00Z"
                    }) {
                        calendarEvent {
                            id
                            recurrenceRule
                        }
                        errors { __typename }
                    }
                }
                """
        };
        var createResponse = await _client.PostAsJsonAsync("/graphql", mutation);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        createContent.Should().Contain("FREQ=DAILY;COUNT=5");

        // Query for expanded occurrences
        var rangeQuery = new
        {
            query = $$"""
                {
                    eventsForRange(
                        calendarIds: ["{{calendarId}}"]
                        rangeStart: "2026-03-01T00:00:00Z"
                        rangeEnd: "2026-03-06T00:00:00Z"
                        timezone: "UTC"
                    ) {
                        calendarEventId
                        title
                        startAt
                        endAt
                        isRecurring
                        isException
                    }
                }
                """
        };

        var response = await _client.PostAsJsonAsync("/graphql", rangeQuery);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"isRecurring\":true");
        content.Should().Contain("\"title\":\"Daily Standup\"");
        content.Should().NotContain("\"errors\"");
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private async Task<string> CreateCalendarAsync()
    {
        var spaceId = await CreateSpaceAsync().ConfigureAwait(false);
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

    private async Task<string> CreateCalendarEventAsync(string calendarId)
    {
        var title = $"Evt-{Guid.NewGuid():N}";
        var mutation = new
        {
            query = $$"""
                mutation {
                    createCalendarEvent(input: {
                        calendarId: "{{calendarId}}"
                        title: "{{title}}"
                        isAllDay: false
                        startAt: "2026-03-05T10:00:00Z"
                        endAt: "2026-03-05T11:00:00Z"
                        timezone: "UTC"
                    }) {
                        calendarEvent { id }
                        errors { __typename }
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", mutation).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return ExtractId(content);
    }

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
