using System.Net;
using System.Net.Http.Json;
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
}
