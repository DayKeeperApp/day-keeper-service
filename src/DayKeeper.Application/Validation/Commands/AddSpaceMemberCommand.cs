using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for adding a member to a space.</summary>
public sealed record AddSpaceMemberCommand(Guid SpaceId, Guid UserId, SpaceRole Role);
