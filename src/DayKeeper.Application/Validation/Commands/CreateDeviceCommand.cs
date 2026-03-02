using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Validation.Commands;

/// <summary>
/// Validation command for registering a new device.
/// </summary>
public sealed record CreateDeviceCommand(
    Guid UserId,
    string DeviceName,
    DevicePlatform Platform,
    string FcmToken);
