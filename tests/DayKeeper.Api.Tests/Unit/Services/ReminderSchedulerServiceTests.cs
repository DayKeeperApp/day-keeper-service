using System.Collections.Specialized;
using DayKeeper.Infrastructure.Jobs;
using DayKeeper.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using Quartz.Impl;

namespace DayKeeper.Api.Tests.Unit.Services;

public sealed class ReminderSchedulerServiceTests
    : IClassFixture<ReminderSchedulerServiceTests.Fixture>
{
    private readonly ReminderSchedulerService _sut;
    private readonly ISchedulerFactory _schedulerFactory;

    public ReminderSchedulerServiceTests(Fixture fixture)
    {
        _schedulerFactory = fixture.SchedulerFactory;
        var logger = Substitute.For<ILogger<ReminderSchedulerService>>();
        _sut = new ReminderSchedulerService(_schedulerFactory, logger);
    }

    // ── ScheduleReminderAsync ───────────────────────────────────────

    [Fact]
    public async Task ScheduleReminderAsync_CreatesJobAndTrigger()
    {
        var reminderId = Guid.NewGuid();
        var fireAt = DateTime.UtcNow.AddMinutes(30);

        await _sut.ScheduleReminderAsync(reminderId, fireAt);

        var scheduler = await _schedulerFactory.GetScheduler();
        var jobKey = new JobKey($"reminder-{reminderId}", "reminders");
        var jobExists = await scheduler.CheckExists(jobKey);
        jobExists.Should().BeTrue();

        var triggerKey = new TriggerKey($"reminder-{reminderId}", "reminders");
        var trigger = await scheduler.GetTrigger(triggerKey);
        trigger.Should().NotBeNull();
        trigger!.StartTimeUtc.Should().Be(new DateTimeOffset(fireAt, TimeSpan.Zero));
    }

    [Fact]
    public async Task ScheduleReminderAsync_StoresReminderIdInJobDataMap()
    {
        var reminderId = Guid.NewGuid();
        var fireAt = DateTime.UtcNow.AddMinutes(30);

        await _sut.ScheduleReminderAsync(reminderId, fireAt);

        var scheduler = await _schedulerFactory.GetScheduler();
        var jobKey = new JobKey($"reminder-{reminderId}", "reminders");
        var jobDetail = await scheduler.GetJobDetail(jobKey);
        jobDetail.Should().NotBeNull();
        jobDetail!.JobDataMap.GetString(ReminderNotificationJob.ReminderIdKey)
            .Should().Be(reminderId.ToString());
    }

    [Fact]
    public async Task ScheduleReminderAsync_ReplacesExistingJob_WhenCalledTwice()
    {
        var reminderId = Guid.NewGuid();
        var originalFireAt = DateTime.UtcNow.AddMinutes(30);
        var updatedFireAt = DateTime.UtcNow.AddMinutes(60);

        await _sut.ScheduleReminderAsync(reminderId, originalFireAt);
        await _sut.ScheduleReminderAsync(reminderId, updatedFireAt);

        var scheduler = await _schedulerFactory.GetScheduler();
        var triggerKey = new TriggerKey($"reminder-{reminderId}", "reminders");
        var trigger = await scheduler.GetTrigger(triggerKey);
        trigger.Should().NotBeNull();
        trigger!.StartTimeUtc.Should().Be(new DateTimeOffset(updatedFireAt, TimeSpan.Zero));
    }

    // ── CancelReminderAsync ─────────────────────────────────────────

    [Fact]
    public async Task CancelReminderAsync_RemovesScheduledJob()
    {
        var reminderId = Guid.NewGuid();
        var fireAt = DateTime.UtcNow.AddMinutes(30);

        await _sut.ScheduleReminderAsync(reminderId, fireAt);
        await _sut.CancelReminderAsync(reminderId);

        var scheduler = await _schedulerFactory.GetScheduler();
        var jobKey = new JobKey($"reminder-{reminderId}", "reminders");
        var jobExists = await scheduler.CheckExists(jobKey);
        jobExists.Should().BeFalse();
    }

    [Fact]
    public async Task CancelReminderAsync_DoesNotThrow_WhenJobDoesNotExist()
    {
        var reminderId = Guid.NewGuid();

        var act = () => _sut.CancelReminderAsync(reminderId);

        await act.Should().NotThrowAsync();
    }

    // ── Fixture ─────────────────────────────────────────────────────

    /// <summary>
    /// Shared fixture providing a real in-memory Quartz scheduler.
    /// Uses a unique scheduler name to avoid collisions with other test classes
    /// (same pattern as QuartzSchedulerRegistrationTests).
    /// </summary>
    public sealed class Fixture : IAsyncLifetime
    {
        public ISchedulerFactory SchedulerFactory { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            var props = new NameValueCollection
            {
                ["quartz.scheduler.instanceName"] = $"ReminderSchedulerTest_{Guid.NewGuid():N}",
                ["quartz.jobStore.type"] = "Quartz.Simpl.RAMJobStore, Quartz",
            };

            SchedulerFactory = new StdSchedulerFactory(props);
            var scheduler = await SchedulerFactory.GetScheduler().ConfigureAwait(false);
            await scheduler.Start().ConfigureAwait(false);
        }

        public async Task DisposeAsync()
        {
            try
            {
                var scheduler = await SchedulerFactory.GetScheduler().ConfigureAwait(false);
                if (!scheduler.IsShutdown)
                {
                    await scheduler.Shutdown(waitForJobsToComplete: false).ConfigureAwait(false);
                }
            }
            catch (ObjectDisposedException)
            {
                // Safe to ignore — scheduler may already be torn down.
            }
        }
    }
}
