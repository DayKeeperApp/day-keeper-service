namespace DayKeeper.Domain.Enums;

/// <summary>
/// Identifies the operating platform of a registered <see cref="Entities.Device"/>.
/// </summary>
public enum DevicePlatform
{
    /// <summary>An Android device.</summary>
    Android = 0,

    /// <summary>An Apple iOS device.</summary>
    Ios = 1,

    /// <summary>A web browser client.</summary>
    Web = 2,
}
