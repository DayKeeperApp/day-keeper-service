using DayKeeper.Infrastructure.Jobs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;

namespace DayKeeper.Api.Tests.Unit.Services;

public sealed class ReminderNotificationJobTests
{
    private readonly ILogger<ReminderNotificationJob> _logger;
    private readonly ReminderNotificationJob _sut;

    public ReminderNotificationJobTests()
    {
        _logger = Substitute.For<ILogger<ReminderNotificationJob>>();
        _sut = new ReminderNotificationJob(_logger);
    }

    [Fact]
    public async Task Execute_WithValidReminderId_DoesNotThrow()
    {
        var reminderId = Guid.NewGuid();
        var context = CreateJobContext(reminderId.ToString());

        var act = () => _sut.Execute(context);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Execute_WithMissingReminderId_DoesNotThrow()
    {
        var context = CreateJobContext(null);

        var act = () => _sut.Execute(context);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Execute_WithInvalidGuid_DoesNotThrow()
    {
        var context = CreateJobContext("not-a-guid");

        var act = () => _sut.Execute(context);

        await act.Should().NotThrowAsync();
    }

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
}
