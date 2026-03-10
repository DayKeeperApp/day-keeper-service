using DayKeeper.Api.Tests.Helpers;
using DayKeeper.Application.DTOs.Notifications;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Jobs;
using DayKeeper.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DayKeeper.Api.Tests.Unit.Services;

public sealed class ReminderNotificationJobTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DayKeeperDbContext _dbContext;
    private readonly INotificationSender _notificationSender;
    private readonly ReminderNotificationJob _sut;

    public ReminderNotificationJobTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DayKeeperDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new DayKeeperDbContext(options, new TestTenantContext());
        _dbContext.Database.EnsureCreated();

        _notificationSender = Substitute.For<INotificationSender>();
        _notificationSender
            .SendAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<PushNotification>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationResult(0, 0, 0, []));

        var logger = Substitute.For<ILogger<ReminderNotificationJob>>();
        _sut = new ReminderNotificationJob(logger, _dbContext, _notificationSender);
    }

    // ── Invalid / Missing ReminderId ─────────────────────────────────

    [Fact]
    public async Task Execute_WithMissingReminderId_DoesNotThrow()
    {
        var context = CreateJobContext(null);

        var act = () => _sut.Execute(context);

        await act.Should().NotThrowAsync();
        await _notificationSender.DidNotReceiveWithAnyArgs()
            .SendAsync(default!, default!, default);
    }

    [Fact]
    public async Task Execute_WithInvalidGuid_DoesNotThrow()
    {
        var context = CreateJobContext("not-a-guid");

        var act = () => _sut.Execute(context);

        await act.Should().NotThrowAsync();
        await _notificationSender.DidNotReceiveWithAnyArgs()
            .SendAsync(default!, default!, default);
    }

    // ── Reminder Not Found ───────────────────────────────────────────

    [Fact]
    public async Task Execute_WhenReminderNotFound_DoesNotSend()
    {
        var context = CreateJobContext(Guid.NewGuid().ToString());

        await _sut.Execute(context);

        await _notificationSender.DidNotReceiveWithAnyArgs()
            .SendAsync(default!, default!, default);
    }

    // ── Non-Push Reminder ────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithEmailMethod_SkipsNotification()
    {
        var (reminder, _) = SeedReminderGraph(ReminderMethod.Email);
        var context = CreateJobContext(reminder.Id.ToString());

        await _sut.Execute(context);

        await _notificationSender.DidNotReceiveWithAnyArgs()
            .SendAsync(default!, default!, default);
    }

    [Fact]
    public async Task Execute_WithInAppMethod_SkipsNotification()
    {
        var (reminder, _) = SeedReminderGraph(ReminderMethod.InApp);
        var context = CreateJobContext(reminder.Id.ToString());

        await _sut.Execute(context);

        await _notificationSender.DidNotReceiveWithAnyArgs()
            .SendAsync(default!, default!, default);
    }

    // ── No Devices ───────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithNoDevicesInSpace_SkipsNotification()
    {
        var (reminder, _) = SeedReminderGraph(ReminderMethod.Push, deviceCount: 0);
        var context = CreateJobContext(reminder.Id.ToString());

        await _sut.Execute(context);

        await _notificationSender.DidNotReceiveWithAnyArgs()
            .SendAsync(default!, default!, default);
    }

    // ── Happy Path ───────────────────────────────────────────────────

    [Fact]
    public async Task Execute_WithValidPushReminder_SendsToAllDevices()
    {
        var (reminder, devices) = SeedReminderGraph(ReminderMethod.Push, deviceCount: 2);
        _notificationSender
            .SendAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<PushNotification>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationResult(2, 2, 0, []));

        var context = CreateJobContext(reminder.Id.ToString());
        await _sut.Execute(context);

        await _notificationSender.Received(1)
            .SendAsync(
                Arg.Is<IReadOnlyList<string>>(tokens =>
                    tokens.Count == 2
                    && tokens.Contains(devices[0].FcmToken)
                    && tokens.Contains(devices[1].FcmToken)),
                Arg.Any<PushNotification>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_WithValidPushReminder_BuildsCorrectPayload()
    {
        var (reminder, _) = SeedReminderGraph(ReminderMethod.Push, deviceCount: 1);
        _notificationSender
            .SendAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<PushNotification>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationResult(1, 1, 0, []));

        var context = CreateJobContext(reminder.Id.ToString());
        await _sut.Execute(context);

        await _notificationSender.Received(1)
            .SendAsync(
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Is<PushNotification>(n =>
                    n.Title == "Test Event"
                    && n.Body == "Starting in 15 minutes"
                    && n.Data != null
                    && n.Data["eventId"] == reminder.CalendarEventId.ToString()
                    && n.Data["reminderId"] == reminder.Id.ToString()),
                Arg.Any<CancellationToken>());
    }

    // ── Multiple Users / Devices ─────────────────────────────────────

    [Fact]
    public async Task Execute_WithMultipleUsersAndDevices_CollectsAllTokens()
    {
        var (reminder, _) = SeedReminderGraph(
            ReminderMethod.Push, userCount: 2, deviceCount: 2);
        _notificationSender
            .SendAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<PushNotification>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationResult(4, 4, 0, []));

        var context = CreateJobContext(reminder.Id.ToString());
        await _sut.Execute(context);

        await _notificationSender.Received(1)
            .SendAsync(
                Arg.Is<IReadOnlyList<string>>(tokens => tokens.Count == 4),
                Arg.Any<PushNotification>(),
                Arg.Any<CancellationToken>());
    }

    // ── Stale Token Cleanup ──────────────────────────────────────────

    [Fact]
    public async Task Execute_WithStaleTokens_SoftDeletesDevices()
    {
        var (reminder, devices) = SeedReminderGraph(ReminderMethod.Push, deviceCount: 2);
        var staleToken = devices[0].FcmToken;

        _notificationSender
            .SendAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<PushNotification>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationResult(2, 1, 1, [staleToken]));

        var context = CreateJobContext(reminder.Id.ToString());
        await _sut.Execute(context);

        var staleDevice = await _dbContext.Set<Device>()
            .IgnoreQueryFilters()
            .FirstAsync(d => d.FcmToken == staleToken);
        staleDevice.DeletedAt.Should().NotBeNull();

        var healthyDevice = await _dbContext.Set<Device>()
            .FirstAsync(d => d.FcmToken == devices[1].FcmToken);
        healthyDevice.DeletedAt.Should().BeNull();
    }

    // ── Notification Preference Filtering ────────────────────────────

    [Fact]
    public async Task Execute_WhenNotifyEventsDisabled_SkipsDevice()
    {
        var (reminder, devices) = SeedReminderGraph(ReminderMethod.Push, deviceCount: 2);

        // Disable event notifications on the first device.
        _dbContext.Set<DeviceNotificationPreference>().Add(new DeviceNotificationPreference
        {
            TenantId = devices[0].TenantId,
            DeviceId = devices[0].Id,
            NotifyEvents = false,
        });

        // Leave the second device with default preferences (events enabled).
        _dbContext.Set<DeviceNotificationPreference>().Add(new DeviceNotificationPreference
        {
            TenantId = devices[1].TenantId,
            DeviceId = devices[1].Id,
        });
        _dbContext.SaveChanges();

        _notificationSender
            .SendAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<PushNotification>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationResult(1, 1, 0, []));

        var context = CreateJobContext(reminder.Id.ToString());
        await _sut.Execute(context);

        await _notificationSender.Received(1)
            .SendAsync(
                Arg.Is<IReadOnlyList<string>>(tokens =>
                    tokens.Count == 1
                    && tokens.Contains(devices[1].FcmToken)),
                Arg.Any<PushNotification>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_WhenAllDevicesHaveNotifyEventsDisabled_SkipsNotification()
    {
        var (reminder, devices) = SeedReminderGraph(ReminderMethod.Push, deviceCount: 1);

        _dbContext.Set<DeviceNotificationPreference>().Add(new DeviceNotificationPreference
        {
            TenantId = devices[0].TenantId,
            DeviceId = devices[0].Id,
            NotifyEvents = false,
        });
        _dbContext.SaveChanges();

        var context = CreateJobContext(reminder.Id.ToString());
        await _sut.Execute(context);

        await _notificationSender.DidNotReceiveWithAnyArgs()
            .SendAsync(default!, default!, default);
    }

    [Fact]
    public async Task Execute_WhenDeviceHasNoPreference_SendsNotification()
    {
        // Device without a NotificationPreference row should still receive notifications.
        var (reminder, _) = SeedReminderGraph(ReminderMethod.Push, deviceCount: 1);
        _notificationSender
            .SendAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<PushNotification>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationResult(1, 1, 0, []));

        var context = CreateJobContext(reminder.Id.ToString());
        await _sut.Execute(context);

        await _notificationSender.Received(1)
            .SendAsync(
                Arg.Is<IReadOnlyList<string>>(tokens => tokens.Count == 1),
                Arg.Any<PushNotification>(),
                Arg.Any<CancellationToken>());
    }

    // ── Token Uniqueness ────────────────────────────────────────────
    // Duplicate FCM tokens across users are prevented by the database unique
    // index on Device.FcmToken, so no deduplication test is needed here.
    // The Distinct() call in the job is a defensive safety net only.

    // ── Helpers ──────────────────────────────────────────────────────

    private static IJobExecutionContext CreateJobContext(string? reminderIdValue)
    {
        var context = Substitute.For<IJobExecutionContext>();
        var dataMap = new JobDataMap();
        if (reminderIdValue is not null)
        {
            dataMap.Put(ReminderNotificationJob.ReminderIdKey, reminderIdValue);
        }

        context.MergedJobDataMap.Returns(dataMap);
        context.FireTimeUtc.Returns(DateTimeOffset.UtcNow);
        return context;
    }

    private (EventReminder Reminder, List<Device> Devices) SeedReminderGraph(
        ReminderMethod method,
        int userCount = 1,
        int deviceCount = 1)
    {
        var tenantId = Guid.NewGuid();
        _dbContext.Set<Tenant>().Add(
            new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test" });

        var space = new Space
        {
            TenantId = tenantId,
            Name = "Shared",
            NormalizedName = "shared",
            SpaceType = SpaceType.Shared,
        };
        _dbContext.Set<Space>().Add(space);

        var allDevices = SeedUsersWithDevices(
            tenantId, space.Id, userCount, deviceCount);

        var reminder = SeedCalendarWithReminder(space.Id, method);

        _dbContext.SaveChanges();
        return (reminder, allDevices);
    }

    private List<Device> SeedUsersWithDevices(
        Guid tenantId, Guid spaceId, int userCount, int deviceCount)
    {
        var allDevices = new List<Device>();

        for (var u = 0; u < userCount; u++)
        {
            var user = new User
            {
                TenantId = tenantId,
                DisplayName = $"User {u}",
                Email = $"user{u}@test.com",
                Timezone = "UTC",
                WeekStart = WeekStart.Monday,
            };
            _dbContext.Set<User>().Add(user);
            _dbContext.Set<SpaceMembership>().Add(new SpaceMembership
            {
                SpaceId = spaceId,
                UserId = user.Id,
                Role = SpaceRole.Owner,
            });

            for (var d = 0; d < deviceCount; d++)
            {
                var device = new Device
                {
                    TenantId = tenantId,
                    UserId = user.Id,
                    DeviceName = $"Device {u}-{d}",
                    Platform = DevicePlatform.Android,
                    FcmToken = $"token-{Guid.NewGuid():N}",
                };
                _dbContext.Set<Device>().Add(device);
                allDevices.Add(device);
            }
        }

        return allDevices;
    }

    private EventReminder SeedCalendarWithReminder(Guid spaceId, ReminderMethod method)
    {
        var calendar = new Calendar
        {
            SpaceId = spaceId,
            Name = "Main",
            NormalizedName = "main",
            Color = "#FF0000",
            IsDefault = true,
        };
        _dbContext.Set<Calendar>().Add(calendar);

        var calendarEvent = new CalendarEvent
        {
            CalendarId = calendar.Id,
            Title = "Test Event",
            StartAt = DateTime.UtcNow.AddMinutes(15),
            EndAt = DateTime.UtcNow.AddHours(1),
            Timezone = "UTC",
        };
        _dbContext.Set<CalendarEvent>().Add(calendarEvent);

        var reminder = new EventReminder
        {
            CalendarEventId = calendarEvent.Id,
            MinutesBefore = 15,
            Method = method,
        };
        _dbContext.Set<EventReminder>().Add(reminder);

        return reminder;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
