namespace DayKeeper.Application.Exceptions;

/// <summary>
/// Thrown when a device registration is attempted with an FCM token
/// that is already registered to another device record.
/// </summary>
public sealed class DuplicateFcmTokenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateFcmTokenException"/> class.
    /// </summary>
    /// <param name="fcmToken">The token that collides.</param>
    public DuplicateFcmTokenException(string fcmToken)
        : base($"A device with FCM token '{fcmToken}' is already registered.")
    {
        FcmToken = fcmToken;
    }

    /// <summary>The FCM token that collides.</summary>
    public string FcmToken { get; }
}
