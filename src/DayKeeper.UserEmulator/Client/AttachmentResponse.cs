namespace DayKeeper.UserEmulator.Client;

public sealed record AttachmentResponse(
    Guid Id,
    Guid TenantId,
    Guid? CalendarEventId,
    Guid? TaskItemId,
    Guid? PersonId,
    string FileName,
    string ContentType,
    long FileSize,
    DateTime CreatedAt);
