namespace DayKeeper.Domain.Enums;

/// <summary>
/// Specifies how an <see cref="Entities.EventReminder"/> is delivered to the user.
/// </summary>
public enum ReminderMethod
{
    /// <summary>Push notification sent to the user's device(s).</summary>
    Push = 0,

    /// <summary>Reminder sent via email.</summary>
    Email = 1,

    /// <summary>In-app notification displayed within the application.</summary>
    InApp = 2,
}
