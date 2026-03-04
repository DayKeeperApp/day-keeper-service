using DayKeeper.Application.DTOs.Notifications;
using DayKeeper.Application.Interfaces;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="INotificationSender"/>
/// backed by Firebase Cloud Messaging (FCM) via the Firebase Admin SDK.
/// </summary>
public sealed partial class FcmNotificationSender(
    ILogger<FcmNotificationSender> logger) : INotificationSender
{
    private readonly ILogger<FcmNotificationSender> _logger = logger;

    /// <inheritdoc />
    public async Task<NotificationResult> SendAsync(
        IReadOnlyList<string> fcmTokens,
        PushNotification notification,
        CancellationToken cancellationToken = default)
    {
        if (fcmTokens.Count == 0)
        {
            LogNoTokens(_logger);
            return new NotificationResult(0, 0, 0, []);
        }

        var message = new MulticastMessage
        {
            Tokens = fcmTokens.ToList(),
            Notification = new Notification
            {
                Title = notification.Title,
                Body = notification.Body,
            },
        };

        if (notification.Data is not null)
        {
            message.Data = notification.Data.ToDictionary(
                kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);
        }

        var response = await FirebaseMessaging.DefaultInstance
            .SendEachForMulticastAsync(message, cancellationToken)
            .ConfigureAwait(false);

        var staleTokens = new List<string>();

        for (var i = 0; i < response.Responses.Count; i++)
        {
            var sendResponse = response.Responses[i];
            if (sendResponse.IsSuccess)
            {
                continue;
            }

            var token = fcmTokens[i];
            var exception = sendResponse.Exception;

            if (exception?.MessagingErrorCode == MessagingErrorCode.Unregistered)
            {
                staleTokens.Add(token);
                LogStaleToken(_logger, token);
            }
            else
            {
                LogSendFailure(_logger, token, exception?.MessagingErrorCode?.ToString(),
                    exception?.Message);
            }
        }

        LogSendComplete(_logger, response.SuccessCount, response.FailureCount);

        return new NotificationResult(
            fcmTokens.Count,
            response.SuccessCount,
            response.FailureCount,
            staleTokens);
    }

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "No FCM tokens provided; skipping notification send.")]
    private static partial void LogNoTokens(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "FCM token {Token} is stale (Unregistered); will be removed.")]
    private static partial void LogStaleToken(ILogger logger, string token);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to send to FCM token {Token}. ErrorCode: {ErrorCode}, Message: {ErrorMessage}")]
    private static partial void LogSendFailure(ILogger logger, string token,
        string? errorCode, string? errorMessage);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "FCM multicast complete. Success: {SuccessCount}, Failure: {FailureCount}.")]
    private static partial void LogSendComplete(ILogger logger, int successCount, int failureCount);
}
