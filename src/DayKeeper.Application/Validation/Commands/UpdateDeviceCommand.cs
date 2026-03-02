namespace DayKeeper.Application.Validation.Commands;

/// <summary>
/// Validation command for updating an existing device registration.
/// </summary>
public sealed record UpdateDeviceCommand(
    Guid Id,
    string? DeviceName,
    string? FcmToken,
    DateTime? LastSyncAt);
