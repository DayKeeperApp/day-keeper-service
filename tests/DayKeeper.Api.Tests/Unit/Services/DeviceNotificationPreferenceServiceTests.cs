using DayKeeper.Api.Tests.Helpers;
using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Persistence;
using DayKeeper.Infrastructure.Persistence.Repositories;
using DayKeeper.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Services;

public sealed class DeviceNotificationPreferenceServiceTests : IDisposable
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _deviceId = Guid.NewGuid();
    private static readonly DateTime _fixedTime =
        new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DayKeeperDbContext _context;
    private readonly DeviceNotificationPreferenceService _sut;

    public DeviceNotificationPreferenceServiceTests()
    {
        var tenantContext = new TestTenantContext { CurrentTenantId = _tenantId };
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(_fixedTime);

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DayKeeperDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new DayKeeperDbContext(options, tenantContext);
        _context.Database.EnsureCreated();

        var repository = new Repository<DeviceNotificationPreference>(_context, dateTimeProvider);

        SeedData();

        _sut = new DeviceNotificationPreferenceService(repository, _context);
    }

    private void SeedData()
    {
        _context.Set<Tenant>().Add(new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
        });

        var user = new User
        {
            TenantId = _tenantId,
            DisplayName = "Test User",
            Email = "test@example.com",
            Timezone = "UTC",
            WeekStart = WeekStart.Monday,
        };
        _context.Set<User>().Add(user);

        _context.Set<Device>().Add(new Device
        {
            Id = _deviceId,
            TenantId = _tenantId,
            UserId = user.Id,
            DeviceName = "Test Device",
            Platform = DevicePlatform.Android,
            FcmToken = "test-token",
        });

        _context.Set<DeviceNotificationPreference>().Add(new DeviceNotificationPreference
        {
            TenantId = _tenantId,
            DeviceId = _deviceId,
        });

        _context.SaveChanges();
    }

    // ── GetByDeviceIdAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetByDeviceIdAsync_ReturnsPreference()
    {
        var result = await _sut.GetByDeviceIdAsync(_deviceId);

        result.Should().NotBeNull();
        result.DeviceId.Should().Be(_deviceId);
        result.NotifyEvents.Should().BeTrue();
        result.DndEnabled.Should().BeFalse();
        result.DefaultReminderLeadTime.Should().Be(ReminderLeadTime.FifteenMin);
    }

    [Fact]
    public async Task GetByDeviceIdAsync_ThrowsEntityNotFound_ForUnknownDevice()
    {
        var act = () => _sut.GetByDeviceIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PartialUpdate_OnlyChangesSpecifiedFields()
    {
        var result = await _sut.UpdateAsync(
            _deviceId,
            dndEnabled: true,
            dndStartTime: null,
            dndEndTime: null,
            defaultReminderLeadTime: null,
            notificationSound: null,
            notifyEvents: null,
            notifyTasks: null,
            notifyLists: null,
            notifyPeople: null);

        result.DndEnabled.Should().BeTrue();
        result.DndStartTime.Should().Be(new TimeOnly(22, 0));
        result.DndEndTime.Should().Be(new TimeOnly(7, 0));
        result.NotifyEvents.Should().BeTrue();
        result.DefaultReminderLeadTime.Should().Be(ReminderLeadTime.FifteenMin);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAllFields()
    {
        var result = await _sut.UpdateAsync(
            _deviceId,
            dndEnabled: true,
            dndStartTime: new TimeOnly(23, 30),
            dndEndTime: new TimeOnly(6, 0),
            defaultReminderLeadTime: ReminderLeadTime.ThirtyMin,
            notificationSound: NotificationSound.Silent,
            notifyEvents: false,
            notifyTasks: false,
            notifyLists: false,
            notifyPeople: true);

        result.DndEnabled.Should().BeTrue();
        result.DndStartTime.Should().Be(new TimeOnly(23, 30));
        result.DndEndTime.Should().Be(new TimeOnly(6, 0));
        result.DefaultReminderLeadTime.Should().Be(ReminderLeadTime.ThirtyMin);
        result.NotificationSound.Should().Be(NotificationSound.Silent);
        result.NotifyEvents.Should().BeFalse();
        result.NotifyTasks.Should().BeFalse();
        result.NotifyLists.Should().BeFalse();
        result.NotifyPeople.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ThrowsEntityNotFound_ForUnknownDevice()
    {
        var act = () => _sut.UpdateAsync(
            Guid.NewGuid(),
            dndEnabled: true,
            dndStartTime: null,
            dndEndTime: null,
            defaultReminderLeadTime: null,
            notificationSound: null,
            notifyEvents: null,
            notifyTasks: null,
            notifyLists: null,
            notifyPeople: null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
