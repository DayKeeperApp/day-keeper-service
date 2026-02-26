using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="SpaceMembership"/> operations.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class SpaceMembershipMutations
{
    /// <summary>Adds a user as a member of a space.</summary>
    [Error<EntityNotFoundException>]
    [Error<DuplicateMembershipException>]
    public Task<SpaceMembership> AddSpaceMemberAsync(
        Guid spaceId,
        Guid userId,
        SpaceRole role,
        ISpaceService spaceService,
        CancellationToken cancellationToken)
    {
        return spaceService.AddMemberAsync(spaceId, userId, role, cancellationToken);
    }

    /// <summary>Changes the role of an existing space member.</summary>
    [Error<EntityNotFoundException>]
    [Error<BusinessRuleViolationException>]
    public Task<SpaceMembership> UpdateSpaceMemberRoleAsync(
        Guid spaceId,
        Guid userId,
        SpaceRole newRole,
        ISpaceService spaceService,
        CancellationToken cancellationToken)
    {
        return spaceService.UpdateMemberRoleAsync(spaceId, userId, newRole, cancellationToken);
    }

    /// <summary>Removes a user's membership from a space.</summary>
    [Error<EntityNotFoundException>]
    [Error<BusinessRuleViolationException>]
    public Task<bool> RemoveSpaceMemberAsync(
        Guid spaceId,
        Guid userId,
        ISpaceService spaceService,
        CancellationToken cancellationToken)
    {
        return spaceService.RemoveMemberAsync(spaceId, userId, cancellationToken);
    }
}
