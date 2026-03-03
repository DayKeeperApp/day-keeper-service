using DayKeeper.Application.DTOs.Notifications;
using DayKeeper.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace DayKeeper.Api.Tests.Unit.Services;

public sealed class FcmNotificationSenderTests
{
    private readonly FcmNotificationSender _sut;

    public FcmNotificationSenderTests()
    {
        var logger = Substitute.For<ILogger<FcmNotificationSender>>();
        _sut = new FcmNotificationSender(logger);
    }

    [Fact]
    public async Task SendAsync_WithEmptyTokenList_ReturnsZeroCounts()
    {
        var notification = new PushNotification("Title", "Body");

        var result = await _sut.SendAsync([], notification);

        result.TotalCount.Should().Be(0);
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(0);
        result.StaleTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_WithEmptyTokenList_DoesNotThrow()
    {
        var notification = new PushNotification("Title", "Body");

        var act = () => _sut.SendAsync([], notification);

        await act.Should().NotThrowAsync();
    }
}
