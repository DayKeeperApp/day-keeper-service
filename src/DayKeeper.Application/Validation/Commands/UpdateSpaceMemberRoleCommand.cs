using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for changing a space member's role.</summary>
public sealed record UpdateSpaceMemberRoleCommand(Guid SpaceId, Guid UserId, SpaceRole NewRole);
