using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="Space"/> entities.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class SpaceMutations
{
    /// <summary>Creates a new space within a tenant.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<DuplicateSpaceNameException>]
    public Task<Space> CreateSpaceAsync(
        Guid tenantId,
        string name,
        SpaceType spaceType,
        Guid createdByUserId,
        ISpaceService spaceService,
        CancellationToken cancellationToken)
    {
        return spaceService.CreateSpaceAsync(
            tenantId, name, spaceType, createdByUserId, cancellationToken);
    }

    /// <summary>Updates an existing space.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<DuplicateSpaceNameException>]
    public Task<Space> UpdateSpaceAsync(
        Guid id,
        string? name,
        SpaceType? spaceType,
        ISpaceService spaceService,
        CancellationToken cancellationToken)
    {
        return spaceService.UpdateSpaceAsync(id, name, spaceType, cancellationToken);
    }

    /// <summary>Soft-deletes a space and all its memberships.</summary>
    public Task<bool> DeleteSpaceAsync(
        Guid id,
        ISpaceService spaceService,
        CancellationToken cancellationToken)
    {
        return spaceService.DeleteSpaceAsync(id, cancellationToken);
    }
}
