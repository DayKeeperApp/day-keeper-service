namespace DayKeeper.Application.DTOs.Notifications;

/// <summary>
/// Represents the payload for a push notification to be sent to one or more devices.
/// </summary>
/// <param name="Title">The notification title displayed to the user.</param>
/// <param name="Body">The notification body text.</param>
/// <param name="Data">Optional key-value pairs sent as the data payload (for client-side handling).</param>
public sealed record PushNotification(
    string Title,
    string Body,
    IReadOnlyDictionary<string, string>? Data = null);
