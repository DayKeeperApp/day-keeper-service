using System.Text.Json;
using DayKeeper.Api.Tests.Helpers;
using DayKeeper.Application.DTOs.Sync;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Persistence;
using DayKeeper.Infrastructure.Persistence.Interceptors;
using DayKeeper.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Services;

public sealed class SyncServiceTests : IDisposable
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _otherTenantId = Guid.NewGuid();
    private static readonly Guid _spaceId = Guid.NewGuid();
    private static readonly DateTime _fixedTime =
        new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DayKeeperDbContext _context;
    private readonly TestTenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly SyncSerializer _syncSerializer;
    private readonly SyncService _sut;

    public SyncServiceTests()
    {
        _tenantContext = new TestTenantContext { CurrentTenantId = _tenantId };
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(_fixedTime);
        _syncSerializer = new SyncSerializer();

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DayKeeperDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(
                new AuditFieldsInterceptor(_dateTimeProvider),
                new ChangeLogInterceptor(_dateTimeProvider, _tenantContext))
            .Options;

        _context = new DayKeeperDbContext(options, _tenantContext);
        _context.Database.EnsureCreated();

        _context.Set<Tenant>().Add(new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
        });
        _context.SaveChanges();
        _context.ChangeLogs.ExecuteDelete();
        _context.ChangeTracker.Clear();

        _sut = new SyncService(_context, _tenantContext, _dateTimeProvider, _syncSerializer);
    }

    // ── PullAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task PullAsync_WhenNoChanges_ReturnsEmptyListAndSameCursor()
    {
        var result = await _sut.PullAsync(0, null, null);

        result.Changes.Should().BeEmpty();
        result.Cursor.Should().Be(0);
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task PullAsync_WhenCursorIsNull_ReturnsAllChanges()
    {
        SeedChangeLogs(3, _tenantId);

        var result = await _sut.PullAsync(null, null, null);

        result.Changes.Should().HaveCount(3);
    }

    [Fact]
    public async Task PullAsync_ReturnsChangesAfterCursor()
    {
        SeedChangeLogs(5, _tenantId);

        var allEntries = await _context.ChangeLogs
            .OrderBy(cl => cl.Id).ToListAsync();
        var cursor = allEntries[1].Id;

        var result = await _sut.PullAsync(cursor, null, null);

        result.Changes.Should().HaveCount(3);
        result.Changes.All(c => c.Id > cursor).Should().BeTrue();
    }

    [Fact]
    public async Task PullAsync_CursorIsLastEntryId()
    {
        SeedChangeLogs(3, _tenantId);

        var result = await _sut.PullAsync(null, null, null);

        var lastEntry = await _context.ChangeLogs
            .OrderByDescending(cl => cl.Id).FirstAsync();
        result.Cursor.Should().Be(lastEntry.Id);
    }

    [Fact]
    public async Task PullAsync_RespectsLimit()
    {
        SeedChangeLogs(5, _tenantId);

        var result = await _sut.PullAsync(null, null, 2);

        result.Changes.Should().HaveCount(2);
    }

    [Fact]
    public async Task PullAsync_WhenMoreChangesExist_HasMoreIsTrue()
    {
        SeedChangeLogs(5, _tenantId);

        var result = await _sut.PullAsync(null, null, 3);

        result.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task PullAsync_WhenExactlyLimitChanges_HasMoreIsFalse()
    {
        SeedChangeLogs(3, _tenantId);

        var result = await _sut.PullAsync(null, null, 3);

        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task PullAsync_FiltersByTenantId()
    {
        SeedChangeLogs(2, _tenantId);
        SeedChangeLogs(3, _otherTenantId);

        var result = await _sut.PullAsync(null, null, null);

        result.Changes.Should().HaveCount(2);
    }

    [Fact]
    public async Task PullAsync_IncludesNullTenantEntries()
    {
        SeedChangeLogs(2, _tenantId);
        SeedChangeLogs(1, null);

        var result = await _sut.PullAsync(null, null, null);

        result.Changes.Should().HaveCount(3);
    }

    [Fact]
    public async Task PullAsync_WhenSpaceIdProvided_FiltersToSpace()
    {
        SeedChangeLog(_tenantId, _spaceId);
        SeedChangeLog(_tenantId, Guid.NewGuid());
        SeedChangeLog(_tenantId, null);

        var result = await _sut.PullAsync(null, _spaceId, null);

        result.Changes.Should().ContainSingle();
    }

    [Fact]
    public async Task PullAsync_ClampsLimitToMax1000()
    {
        SeedChangeLogs(5, _tenantId);

        // Limit > 1000 should not crash - just clamped
        var result = await _sut.PullAsync(null, null, 5000);

        result.Changes.Should().HaveCount(5);
    }

    [Fact]
    public async Task PullAsync_ClampsLimitToMin1()
    {
        SeedChangeLogs(3, _tenantId);

        var result = await _sut.PullAsync(null, null, 0);

        result.Changes.Should().ContainSingle();
    }

    [Fact]
    public async Task PullAsync_ReturnsEntityDataForCreatedEntries()
    {
        var user = SeedUser();

        var result = await _sut.PullAsync(null, null, null);

        result.Changes.Should().ContainSingle();
        var entry = result.Changes[0];
        entry.Data.Should().NotBeNull();
        entry.Data!.Value.GetProperty("displayName").GetString()
            .Should().Be(user.DisplayName);
    }

    [Fact]
    public async Task PullAsync_ReturnsNullDataForDeletedEntries()
    {
        SeedChangeLog(_tenantId, null, operation: ChangeOperation.Deleted);

        var result = await _sut.PullAsync(null, null, null);

        result.Changes.Should().ContainSingle();
        result.Changes[0].Data.Should().BeNull();
    }

    // ── PushAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task PushAsync_WhenNoChanges_ReturnsZeroCounts()
    {
        var result = await _sut.PushAsync([]);

        result.AppliedCount.Should().Be(0);
        result.RejectedCount.Should().Be(0);
        result.Conflicts.Should().BeEmpty();
    }

    [Fact]
    public async Task PushAsync_WhenEntityIsNew_AcceptsChange()
    {
        var entityId = Guid.NewGuid();
        var data = SerializeUser(entityId);
        var changes = new[]
        {
            new SyncPushEntry(
                ChangeLogEntityType.User,
                entityId,
                ChangeOperation.Created,
                _fixedTime,
                data),
        };

        var result = await _sut.PushAsync(changes);

        result.AppliedCount.Should().Be(1);
        result.RejectedCount.Should().Be(0);
        result.Conflicts.Should().BeEmpty();
    }

    [Fact]
    public async Task PushAsync_WhenClientIsNewer_AcceptsChange()
    {
        var user = SeedUser();
        SeedChangeLog(_tenantId, null, user.Id, _fixedTime.AddHours(-1));

        var data = SerializeUser(user.Id, displayName: "Updated Name");
        var changes = new[]
        {
            new SyncPushEntry(
                ChangeLogEntityType.User,
                user.Id,
                ChangeOperation.Updated,
                _fixedTime,
                data),
        };

        var result = await _sut.PushAsync(changes);

        result.AppliedCount.Should().Be(1);
        result.Conflicts.Should().BeEmpty();
    }

    [Fact]
    public async Task PushAsync_WhenClientIsOlder_RejectsWithConflict()
    {
        var entityId = Guid.NewGuid();
        var serverTime = _fixedTime.AddHours(1);
        SeedChangeLog(_tenantId, null, entityId, serverTime);

        var changes = new[]
        {
            new SyncPushEntry(
                ChangeLogEntityType.User,
                entityId,
                ChangeOperation.Updated,
                _fixedTime,
                null),
        };

        var result = await _sut.PushAsync(changes);

        result.AppliedCount.Should().Be(0);
        result.RejectedCount.Should().Be(1);
        result.Conflicts.Should().ContainSingle();
        result.Conflicts[0].EntityId.Should().Be(entityId);
        result.Conflicts[0].ClientTimestamp.Should().Be(_fixedTime);
        result.Conflicts[0].ServerTimestamp.Should().Be(serverTime);
    }

    [Fact]
    public async Task PushAsync_WhenTimestampsEqual_AcceptsChange()
    {
        var user = SeedUser();
        SeedChangeLog(_tenantId, null, user.Id, _fixedTime);

        var data = SerializeUser(user.Id, displayName: "Updated Name");
        var changes = new[]
        {
            new SyncPushEntry(
                ChangeLogEntityType.User,
                user.Id,
                ChangeOperation.Updated,
                _fixedTime,
                data),
        };

        var result = await _sut.PushAsync(changes);

        result.AppliedCount.Should().Be(1);
        result.Conflicts.Should().BeEmpty();
    }

    [Fact]
    public async Task PushAsync_Create_AddsEntityToDatabase()
    {
        var entityId = Guid.NewGuid();
        var data = SerializeUser(entityId, displayName: "New User");
        var changes = new[]
        {
            new SyncPushEntry(
                ChangeLogEntityType.User,
                entityId,
                ChangeOperation.Created,
                _fixedTime,
                data),
        };

        await _sut.PushAsync(changes);

        var user = await _context.Set<User>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(u => u.Id == entityId);
        user.Should().NotBeNull();
        user!.DisplayName.Should().Be("New User");
    }

    [Fact]
    public async Task PushAsync_Update_ModifiesExistingEntity()
    {
        var user = SeedUser(displayName: "Original");
        SeedChangeLog(_tenantId, null, user.Id, _fixedTime.AddHours(-1));

        var data = SerializeUser(user.Id, displayName: "Modified");
        var changes = new[]
        {
            new SyncPushEntry(
                ChangeLogEntityType.User,
                user.Id,
                ChangeOperation.Updated,
                _fixedTime,
                data),
        };

        await _sut.PushAsync(changes);

        _context.ChangeTracker.Clear();
        var updated = await _context.Set<User>()
            .IgnoreQueryFilters()
            .SingleAsync(u => u.Id == user.Id);
        updated.DisplayName.Should().Be("Modified");
    }

    [Fact]
    public async Task PushAsync_Delete_SetsDeletedAt()
    {
        var user = SeedUser();
        SeedChangeLog(_tenantId, null, user.Id, _fixedTime.AddHours(-1));

        var changes = new[]
        {
            new SyncPushEntry(
                ChangeLogEntityType.User,
                user.Id,
                ChangeOperation.Deleted,
                _fixedTime,
                null),
        };

        await _sut.PushAsync(changes);

        _context.ChangeTracker.Clear();
        var deleted = await _context.Set<User>()
            .IgnoreQueryFilters()
            .SingleAsync(u => u.Id == user.Id);
        deleted.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PushAsync_CreatesChangeLogEntriesForAccepted()
    {
        var entityId = Guid.NewGuid();
        var data = SerializeUser(entityId);
        var changes = new[]
        {
            new SyncPushEntry(
                ChangeLogEntityType.User,
                entityId,
                ChangeOperation.Created,
                _fixedTime,
                data),
        };

        await _sut.PushAsync(changes);

        var log = await _context.ChangeLogs
            .SingleAsync(cl => cl.EntityId == entityId);
        log.Operation.Should().Be(ChangeOperation.Created);
        log.EntityType.Should().Be(ChangeLogEntityType.User);
        log.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public async Task PushAsync_DoesNotCreateChangeLogForRejected()
    {
        var entityId = Guid.NewGuid();
        SeedChangeLog(_tenantId, null, entityId, _fixedTime.AddHours(1));
        var initialCount = await _context.ChangeLogs.CountAsync();

        var changes = new[]
        {
            new SyncPushEntry(
                ChangeLogEntityType.User,
                entityId,
                ChangeOperation.Updated,
                _fixedTime,
                null),
        };

        await _sut.PushAsync(changes);

        var finalCount = await _context.ChangeLogs.CountAsync();
        finalCount.Should().Be(initialCount);
    }

    [Fact]
    public async Task PushAsync_MixedAcceptedAndRejected()
    {
        var newEntityId = Guid.NewGuid();
        var existingEntityId = Guid.NewGuid();
        SeedChangeLog(_tenantId, null, existingEntityId, _fixedTime.AddHours(1));

        var data = SerializeUser(newEntityId);
        var changes = new[]
        {
            new SyncPushEntry(
                ChangeLogEntityType.User,
                newEntityId,
                ChangeOperation.Created,
                _fixedTime,
                data),
            new SyncPushEntry(
                ChangeLogEntityType.User,
                existingEntityId,
                ChangeOperation.Updated,
                _fixedTime,
                null),
        };

        var result = await _sut.PushAsync(changes);

        result.AppliedCount.Should().Be(1);
        result.RejectedCount.Should().Be(1);
        result.Conflicts.Should().ContainSingle();
    }

    [Fact]
    public async Task PushAsync_CreateWithoutData_IsSkipped()
    {
        var changes = new[]
        {
            new SyncPushEntry(
                ChangeLogEntityType.User,
                Guid.NewGuid(),
                ChangeOperation.Created,
                _fixedTime,
                null),
        };

        var result = await _sut.PushAsync(changes);

        result.AppliedCount.Should().Be(0);
    }

    // ── Seed Helpers ──────────────────────────────────────────────────

    private void SeedChangeLogs(int count, Guid? tenantId)
    {
        for (var i = 0; i < count; i++)
        {
            SeedChangeLog(tenantId, null);
        }
    }

    private void SeedChangeLog(
        Guid? tenantId,
        Guid? spaceId,
        Guid? entityId = null,
        DateTime? timestamp = null,
        ChangeOperation operation = ChangeOperation.Created)
    {
        _context.ChangeLogs.Add(new ChangeLog
        {
            EntityType = ChangeLogEntityType.User,
            EntityId = entityId ?? Guid.NewGuid(),
            Operation = operation,
            TenantId = tenantId,
            SpaceId = spaceId,
            Timestamp = timestamp ?? _fixedTime,
        });
        _context.SaveChanges();
    }

    private User SeedUser(
        Guid? id = null,
        string displayName = "Test User")
    {
        var user = new User
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = _tenantId,
            DisplayName = displayName,
            Email = $"{Guid.NewGuid():N}@test.com",
            Timezone = "America/Chicago",
            WeekStart = WeekStart.Monday,
        };
        _context.Set<User>().Add(user);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        return user;
    }

    private JsonElement SerializeUser(
        Guid id,
        string displayName = "Test User")
    {
        var user = new User
        {
            Id = id,
            TenantId = _tenantId,
            DisplayName = displayName,
            Email = $"{Guid.NewGuid():N}@test.com",
            Timezone = "America/Chicago",
            WeekStart = WeekStart.Monday,
        };
        return _syncSerializer.Serialize(user);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
