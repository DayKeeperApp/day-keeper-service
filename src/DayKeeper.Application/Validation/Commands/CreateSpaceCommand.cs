using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for creating a new space.</summary>
public sealed record CreateSpaceCommand(
    Guid TenantId,
    string Name,
    SpaceType SpaceType,
    Guid CreatedByUserId);
