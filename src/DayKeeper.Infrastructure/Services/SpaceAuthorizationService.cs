using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="ISpaceAuthorizationService"/>.
/// Queries <see cref="SpaceMembership"/> entities to determine user
/// permissions within a space.
/// </summary>
public sealed class SpaceAuthorizationService(
    DbContext dbContext) : ISpaceAuthorizationService
{
    private readonly DbContext _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<SpaceRole?> GetUserRoleAsync(
        Guid spaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var membership = await _dbContext.Set<SpaceMembership>()
            .FirstOrDefaultAsync(
                m => m.SpaceId == spaceId && m.UserId == userId,
                cancellationToken)
            .ConfigureAwait(false);

        return membership?.Role;
    }

    /// <inheritdoc />
    public async Task<bool> HasMinimumRoleAsync(
        Guid spaceId,
        Guid userId,
        SpaceRole minimumRole,
        CancellationToken cancellationToken = default)
    {
        var role = await GetUserRoleAsync(spaceId, userId, cancellationToken)
            .ConfigureAwait(false);

        return role.HasValue && role.Value >= minimumRole;
    }

    /// <inheritdoc />
    public Task<bool> CanViewAsync(
        Guid spaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return HasMinimumRoleAsync(spaceId, userId, SpaceRole.Viewer, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> CanEditAsync(
        Guid spaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return HasMinimumRoleAsync(spaceId, userId, SpaceRole.Editor, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> IsOwnerAsync(
        Guid spaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return HasMinimumRoleAsync(spaceId, userId, SpaceRole.Owner, cancellationToken);
    }

    /// <inheritdoc />
    public async Task EnsureMinimumRoleAsync(
        Guid spaceId,
        Guid userId,
        SpaceRole minimumRole,
        CancellationToken cancellationToken = default)
    {
        var hasRole = await HasMinimumRoleAsync(spaceId, userId, minimumRole, cancellationToken)
            .ConfigureAwait(false);

        if (!hasRole)
        {
            throw new BusinessRuleViolationException(
                "InsufficientSpaceRole",
                $"User '{userId}' does not have the required '{minimumRole}' role in space '{spaceId}'.");
        }
    }
}
