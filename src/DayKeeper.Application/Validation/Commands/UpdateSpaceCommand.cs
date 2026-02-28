using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for updating an existing space.</summary>
public sealed record UpdateSpaceCommand(Guid Id, string? Name, SpaceType? SpaceType);
