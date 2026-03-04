namespace DayKeeper.Application.DTOs.Notifications;

/// <summary>
/// Result of a push notification send operation.
/// </summary>
/// <param name="TotalCount">Total number of tokens targeted.</param>
/// <param name="SuccessCount">Number of tokens that accepted the message.</param>
/// <param name="FailureCount">Number of tokens that rejected the message.</param>
/// <param name="StaleTokens">FCM tokens that should be removed (unregistered devices).</param>
public sealed record NotificationResult(
    int TotalCount,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<string> StaleTokens);
