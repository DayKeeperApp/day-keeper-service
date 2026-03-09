using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DayKeeper.Application.DTOs.Sync;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace DayKeeper.Api.Tests.Integration;

[Collection("Integration")]
public class SyncEndpointTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SyncEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostPull_WithNullCursor_ReturnsOk()
    {
        var request = new SyncPullRequest(null, null, null);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/sync/pull", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostPull_ReturnsExpectedResponseShape()
    {
        var request = new SyncPullRequest(null, null, null);

        var result = await _client.PostAsJsonAsync(
            "/api/v1/sync/pull", request);
        var body = await result.Content
            .ReadFromJsonAsync<SyncPullResponse>();

        body.Should().NotBeNull();
        body!.Changes.Should().NotBeNull();
        body.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task PostPush_WithEmptyChanges_ReturnsOk()
    {
        var request = new SyncPushRequest([]);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/sync/push", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostPush_ReturnsExpectedResponseShape()
    {
        var request = new SyncPushRequest([]);

        var result = await _client.PostAsJsonAsync(
            "/api/v1/sync/push", request);
        var body = await result.Content
            .ReadFromJsonAsync<SyncPushResponse>();

        body.Should().NotBeNull();
        body!.AppliedCount.Should().Be(0);
        body.RejectedCount.Should().Be(0);
        body.Conflicts.Should().BeEmpty();
    }

    [Fact]
    public async Task PostPush_WithNewEntity_ThenPull_ReturnsChange()
    {
        var entityId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<DayKeeperDbContext>();
            db.Set<Tenant>().Add(new Tenant
            {
                Id = tenantId,
                Name = "Push Test Tenant",
                Slug = "push-test-tenant",
            });
            await db.SaveChangesAsync();
        }

        var serializer = _factory.Services
            .GetRequiredService<ISyncSerializer>();
        var data = serializer.Serialize(new User
        {
            Id = entityId,
            TenantId = tenantId,
            DisplayName = "Integration Test User",
            Email = $"{Guid.NewGuid():N}@test.com",
            Timezone = "UTC",
            WeekStart = WeekStart.Monday,
        });

        var pushResponse = await _client.PostAsJsonAsync(
            "/api/v1/sync/push",
            new SyncPushRequest(
            [
                new SyncPushEntry(
                    ChangeLogEntityType.User,
                    entityId,
                    ChangeOperation.Created,
                    DateTime.UtcNow,
                    data),
            ]));
        pushResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pushBody = await pushResponse.Content
            .ReadFromJsonAsync<SyncPushResponse>();
        pushBody!.AppliedCount.Should().Be(1);

        var pullResponse = await _client.PostAsJsonAsync(
            "/api/v1/sync/pull", new SyncPullRequest(null, null, null));
        pullResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pullBody = await pullResponse.Content
            .ReadFromJsonAsync<SyncPullResponse>();
        pullBody!.Changes.Should().Contain(c => c.EntityId == entityId);
    }

    [Fact]
    public async Task PostPush_WithConflict_ReturnsConflictInResponse()
    {
        var entityId = Guid.NewGuid();

        // Seed a server-side ChangeLog entry with a future timestamp
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<DayKeeperDbContext>();
            db.ChangeLogs.Add(new ChangeLog
            {
                EntityType = ChangeLogEntityType.User,
                EntityId = entityId,
                Operation = ChangeOperation.Created,
                TenantId = null,
                SpaceId = null,
                Timestamp = DateTime.UtcNow.AddHours(1),
            });
            await db.SaveChangesAsync();
        }

        var pushRequest = new SyncPushRequest(
        [
            new SyncPushEntry(
                ChangeLogEntityType.User,
                entityId,
                ChangeOperation.Updated,
                DateTime.UtcNow.AddHours(-1),
                null),
        ]);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/sync/push", pushRequest);
        var body = await response.Content
            .ReadFromJsonAsync<SyncPushResponse>();

        body!.RejectedCount.Should().Be(1);
        body.Conflicts.Should().ContainSingle();
        body.Conflicts[0].EntityId.Should().Be(entityId);
    }

    // ── Full sync cycle: GraphQL → Pull ───────────────────────────────

    [Fact]
    public async Task PostPull_AfterGraphQLCreateCalendar_ReturnsCalendarChangeWithCorrectData()
    {
        var spaceId = await CreateSpaceAsync();
        var calendarName = $"SyncCal-{Guid.NewGuid():N}";
        var calendarId = await CreateCalendarAsync(spaceId, calendarName);

        var pull = await PullAsync();

        var entry = pull.Changes.Should()
            .Contain(c => c.EntityId == Guid.Parse(calendarId))
            .Which;
        entry.EntityType.Should().Be(ChangeLogEntityType.Calendar);
        entry.Operation.Should().Be(ChangeOperation.Created);
        entry.Data.Should().NotBeNull();

        var data = entry.Data!.Value;
        data.GetProperty("name").GetString().Should().Be(calendarName);
        data.GetProperty("color").GetString().Should().Be("#4A90D9");
        data.GetProperty("spaceId").GetString().Should().Be(spaceId);
        data.GetProperty("isDefault").GetBoolean().Should().BeFalse();

        // Navigation properties must be excluded by SyncSerializer
        data.TryGetProperty("space", out _).Should().BeFalse();
        data.TryGetProperty("events", out _).Should().BeFalse();
    }

    [Fact]
    public async Task PostPull_AfterGraphQLCreateCalendarEvent_ReturnsEventChangeWithCorrectData()
    {
        var spaceId = await CreateSpaceAsync();
        var calendarId = await CreateCalendarAsync(spaceId);
        var eventTitle = $"SyncEvt-{Guid.NewGuid():N}";
        var eventId = await CreateCalendarEventAsync(calendarId, eventTitle);

        var pull = await PullAsync();

        var entry = pull.Changes.Should()
            .Contain(c => c.EntityId == Guid.Parse(eventId))
            .Which;
        entry.EntityType.Should().Be(ChangeLogEntityType.CalendarEvent);
        entry.Operation.Should().Be(ChangeOperation.Created);
        entry.Data.Should().NotBeNull();

        var data = entry.Data!.Value;
        data.GetProperty("title").GetString().Should().Be(eventTitle);
        data.GetProperty("calendarId").GetString().Should().Be(calendarId);
        data.GetProperty("timezone").GetString().Should().Be("UTC");

        // Navigation excluded
        data.TryGetProperty("calendar", out _).Should().BeFalse();
    }

    // ── Cursor behaviour ──────────────────────────────────────────────

    [Fact]
    public async Task PostPull_WithReturnedCursor_ReturnsNoDuplicateChanges()
    {
        var spaceId = await CreateSpaceAsync();
        await CreateCalendarAsync(spaceId);

        var pull1 = await PullAsync();
        pull1.Changes.Should().NotBeEmpty();
        pull1.Cursor.Should().BeGreaterThan(0);

        var pull2 = await PullAsync(pull1.Cursor);

        pull2.Changes.Should().NotContain(c =>
            pull1.Changes.Any(p1 => p1.Id == c.Id));
    }

    [Fact]
    public async Task PostPull_WithNoNewChanges_ReturnsSameCursor()
    {
        var baseline = await GetBaselineCursorAsync();

        var pull = await PullAsync(baseline);

        pull.Cursor.Should().Be(baseline);
        pull.Changes.Should().BeEmpty();
        pull.HasMore.Should().BeFalse();
    }

    // ── Pagination ────────────────────────────────────────────────────

    [Fact]
    public async Task PostPull_WithSmallLimit_ReturnsHasMoreAndPaginates()
    {
        var baseline = await GetBaselineCursorAsync();

        // Create entities to generate multiple ChangeLog entries
        // Tenant + User + Space + SpaceMembership + Cal1 + Cal2 = 6+ entries
        var spaceId = await CreateSpaceAsync();
        await CreateCalendarAsync(spaceId);
        await CreateCalendarAsync(spaceId);

        var page1 = await PullAsync(baseline, limit: 2);
        page1.Changes.Should().HaveCount(2);
        page1.HasMore.Should().BeTrue();

        var page2 = await PullAsync(page1.Cursor, limit: 100);
        page2.Changes.Should().NotBeEmpty();
        page2.Changes.Should().NotContain(c =>
            page1.Changes.Any(p1 => p1.Id == c.Id));
    }

    // ── Space-scoped pull ─────────────────────────────────────────────

    [Fact]
    public async Task PostPull_WithSpaceIdFilter_ReturnsOnlyMatchingSpaceChanges()
    {
        var baseline = await GetBaselineCursorAsync();

        var spaceIdA = await CreateSpaceAsync();
        var calIdA = await CreateCalendarAsync(spaceIdA);
        var spaceIdB = await CreateSpaceAsync();
        var calIdB = await CreateCalendarAsync(spaceIdB);

        var pull = await PullAsync(baseline, spaceId: Guid.Parse(spaceIdA));

        pull.Changes.Should().Contain(c =>
            c.EntityId == Guid.Parse(calIdA));
        pull.Changes.Should().NotContain(c =>
            c.EntityId == Guid.Parse(calIdB));
        pull.Changes.Should().NotContain(c =>
            c.EntityId == Guid.Parse(spaceIdB));
    }

    // ── Delete via GraphQL ────────────────────────────────────────────

    [Fact]
    public async Task PostPull_AfterGraphQLDelete_ReturnsDeletedOperationWithNullData()
    {
        var spaceId = await CreateSpaceAsync();
        var calendarId = await CreateCalendarAsync(spaceId);

        var afterCreate = await GetBaselineCursorAsync();

        var deleteMutation = new
        {
            query = $$"""
                mutation {
                    deleteCalendar(input: { id: "{{calendarId}}" }) {
                        boolean
                    }
                }
                """
        };
        await _client.PostAsJsonAsync("/graphql", deleteMutation);

        var pull = await PullAsync(afterCreate);

        var entry = pull.Changes.Should()
            .Contain(c => c.EntityId == Guid.Parse(calendarId)
                && c.Operation == ChangeOperation.Deleted)
            .Which;
        entry.Data.Should().BeNull();
    }

    // ── Update via GraphQL ────────────────────────────────────────────

    [Fact]
    public async Task PostPull_AfterGraphQLUpdate_ReturnsUpdatedOperationWithCurrentData()
    {
        var spaceId = await CreateSpaceAsync();
        var calendarId = await CreateCalendarAsync(spaceId);

        var afterCreate = await GetBaselineCursorAsync();

        var updateMutation = new
        {
            query = $$"""
                mutation {
                    updateCalendar(input: {
                        id: "{{calendarId}}"
                        name: "Updated Sync Calendar"
                    }) {
                        calendar { id name }
                        errors { __typename }
                    }
                }
                """
        };
        await _client.PostAsJsonAsync("/graphql", updateMutation);

        var pull = await PullAsync(afterCreate);

        var entry = pull.Changes.Should()
            .Contain(c => c.EntityId == Guid.Parse(calendarId)
                && c.Operation == ChangeOperation.Updated)
            .Which;
        entry.Data.Should().NotBeNull();
        entry.Data!.Value.GetProperty("name").GetString()
            .Should().Be("Updated Sync Calendar");
    }

    // ── Push conflict against GraphQL-created entity ──────────────────

    [Fact]
    public async Task PostPush_WithStaleTimestampAfterGraphQLCreate_ReturnsConflict()
    {
        var spaceId = await CreateSpaceAsync();
        var calendarId = await CreateCalendarAsync(spaceId);

        var serializer = _factory.Services
            .GetRequiredService<ISyncSerializer>();
        var data = serializer.Serialize(new Calendar
        {
            Id = Guid.Parse(calendarId),
            SpaceId = Guid.Parse(spaceId),
            Name = "Stale Update",
            NormalizedName = "stale update",
            Color = "#FF0000",
            IsDefault = false,
        });

        var push = await PushAsync(
        [
            new SyncPushEntry(
                ChangeLogEntityType.Calendar,
                Guid.Parse(calendarId),
                ChangeOperation.Updated,
                DateTime.UtcNow.AddHours(-1),
                data),
        ]);

        push.RejectedCount.Should().Be(1);
        push.Conflicts.Should().ContainSingle();
        push.Conflicts[0].EntityId.Should().Be(Guid.Parse(calendarId));
        push.Conflicts[0].Reason.Should().Be(SyncConflictReason.TimestampConflict);
        push.Conflicts[0].ClientTimestamp.Should()
            .BeBefore(push.Conflicts[0].ServerTimestamp!.Value);
    }

    // ── Push create → GraphQL query ───────────────────────────────────

    [Fact]
    public async Task PostPush_CreateCalendarViaPush_ThenQueryableViaGraphQL()
    {
        var spaceId = await CreateSpaceAsync();
        var newId = Guid.NewGuid();
        var calendarName = $"PushCal-{Guid.NewGuid():N}";

        var serializer = _factory.Services
            .GetRequiredService<ISyncSerializer>();
        var data = serializer.Serialize(new Calendar
        {
            Id = newId,
            SpaceId = Guid.Parse(spaceId),
            Name = calendarName,
            NormalizedName = calendarName.ToLowerInvariant(),
            Color = "#123456",
            IsDefault = false,
        });

        var push = await PushAsync(
        [
            new SyncPushEntry(
                ChangeLogEntityType.Calendar,
                newId,
                ChangeOperation.Created,
                DateTime.UtcNow,
                data),
        ]);
        push.AppliedCount.Should().Be(1);
        push.RejectedCount.Should().Be(0);

        var graphqlQuery = new
        {
            query = $$"""
                {
                    calendarById(id: "{{newId}}") {
                        id
                        name
                        color
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", graphqlQuery);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain($"\"name\":\"{calendarName}\"");
        content.Should().Contain("\"color\":\"#123456\"");
    }

    // ── Push EntityId honoured when Data omits id ─────────────────────

    [Fact]
    public async Task PostPush_WithEntityIdNotInData_UsesEntityIdFromPushEntry()
    {
        var spaceId = await CreateSpaceAsync();
        var newId = Guid.NewGuid();
        var calendarName = $"NoIdCal-{Guid.NewGuid():N}";

        var serializer = _factory.Services
            .GetRequiredService<ISyncSerializer>();

        // Serialize a calendar then strip the "id" property from the JSON.
        var fullData = serializer.Serialize(new Calendar
        {
            Id = Guid.NewGuid(), // deliberately different from newId
            SpaceId = Guid.Parse(spaceId),
            Name = calendarName,
            NormalizedName = calendarName.ToLowerInvariant(),
            Color = "#AABBCC",
            IsDefault = false,
        });

        // Remove "id" from the JSON payload so the service must use EntityId.
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            fullData.GetRawText())!;
        dict.Remove("id");
        var strippedJson = JsonSerializer.SerializeToElement(dict);

        var push = await PushAsync(
        [
            new SyncPushEntry(
                ChangeLogEntityType.Calendar,
                newId,
                ChangeOperation.Created,
                DateTime.UtcNow,
                strippedJson),
        ]);
        push.AppliedCount.Should().Be(1);
        push.RejectedCount.Should().Be(0);

        // Verify the entity is queryable by the EntityId from the push entry.
        var graphqlQuery = new
        {
            query = $$"""
                {
                    calendarById(id: "{{newId}}") {
                        id
                        name
                    }
                }
                """
        };
        var response = await _client.PostAsJsonAsync("/graphql", graphqlQuery);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain($"\"name\":\"{calendarName}\"");
    }

    // ── Duplicate EntityId on Created ────────────────────────────────

    [Fact]
    public async Task PostPush_WithDuplicateEntityId_ReturnsDuplicateEntityConflict()
    {
        var spaceId = await CreateSpaceAsync();
        var calendarId = await CreateCalendarAsync(spaceId);

        var serializer = _factory.Services
            .GetRequiredService<ISyncSerializer>();
        var data = serializer.Serialize(new Calendar
        {
            Id = Guid.Parse(calendarId),
            SpaceId = Guid.Parse(spaceId),
            Name = "Duplicate Push",
            NormalizedName = "duplicate push",
            Color = "#FF0000",
            IsDefault = false,
        });

        var push = await PushAsync(
        [
            new SyncPushEntry(
                ChangeLogEntityType.Calendar,
                Guid.Parse(calendarId),
                ChangeOperation.Created,
                DateTime.UtcNow.AddHours(1), // future timestamp to bypass LWW
                data),
        ]);

        push.AppliedCount.Should().Be(0);
        push.RejectedCount.Should().Be(1);
        push.Conflicts.Should().ContainSingle();
        push.Conflicts[0].EntityId.Should().Be(Guid.Parse(calendarId));
        push.Conflicts[0].Reason.Should().Be(SyncConflictReason.DuplicateEntity);
        push.Conflicts[0].ClientTimestamp.Should().BeNull();
        push.Conflicts[0].ServerTimestamp.Should().BeNull();
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private async Task<SyncPullResponse> PullAsync(
        long? cursor = null, Guid? spaceId = null, int? limit = null)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/sync/pull",
            new SyncPullRequest(cursor, spaceId, limit)).ConfigureAwait(false);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<SyncPullResponse>().ConfigureAwait(false);
        return body!;
    }

    private async Task<SyncPushResponse> PushAsync(
        IReadOnlyList<SyncPushEntry> changes)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/sync/push", new SyncPushRequest(changes)).ConfigureAwait(false);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content
            .ReadFromJsonAsync<SyncPushResponse>().ConfigureAwait(false);
        return body!;
    }

    private async Task<long> GetBaselineCursorAsync()
    {
        var pull = await PullAsync(limit: 1000).ConfigureAwait(false);
        while (pull.HasMore)
        {
            pull = await PullAsync(pull.Cursor, limit: 1000).ConfigureAwait(false);
        }

        return pull.Cursor;
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

    private async Task<string> CreateCalendarAsync(
        string spaceId, string? name = null)
    {
        name ??= $"Cal-{Guid.NewGuid():N}";
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

    private async Task<string> CreateCalendarEventAsync(
        string calendarId, string? title = null)
    {
        title ??= $"Evt-{Guid.NewGuid():N}";
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
