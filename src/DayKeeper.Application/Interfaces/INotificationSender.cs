using DayKeeper.Application.DTOs.Notifications;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Sends push notifications to devices via their FCM registration tokens.
/// </summary>
public interface INotificationSender
{
    /// <summary>
    /// Sends a push notification to the specified FCM registration tokens.
    /// </summary>
    /// <param name="fcmTokens">One or more FCM registration tokens to target.</param>
    /// <param name="notification">The notification payload.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A result summarizing the outcome of the send operation.</returns>
    Task<NotificationResult> SendAsync(
        IReadOnlyList<string> fcmTokens,
        PushNotification notification,
        CancellationToken cancellationToken = default);
}
