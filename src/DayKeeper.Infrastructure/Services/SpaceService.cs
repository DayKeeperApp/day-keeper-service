using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="ISpaceService"/>.
/// Orchestrates business rules for space and membership management
/// using repository abstractions and direct DbContext queries.
/// </summary>
public sealed class SpaceService(
    IRepository<Space> spaceRepository,
    IRepository<SpaceMembership> membershipRepository,
    IRepository<User> userRepository,
    DbContext dbContext) : ISpaceService
{
    private readonly IRepository<Space> _spaceRepository = spaceRepository;
    private readonly IRepository<SpaceMembership> _membershipRepository = membershipRepository;
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly DbContext _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<Space> CreateSpaceAsync(
        Guid tenantId,
        string name,
        SpaceType spaceType,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(createdByUserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            throw new EntityNotFoundException(nameof(User), createdByUserId);
        }

        var normalizedName = name.Trim().ToLowerInvariant();

        var nameExists = await _dbContext.Set<Space>()
            .AnyAsync(s => s.TenantId == tenantId && s.NormalizedName == normalizedName,
                cancellationToken)
            .ConfigureAwait(false);

        if (nameExists)
        {
            throw new DuplicateSpaceNameException(tenantId, normalizedName);
        }

        var space = new Space
        {
            TenantId = tenantId,
            Name = name.Trim(),
            NormalizedName = normalizedName,
            SpaceType = spaceType,
        };

        var createdSpace = await _spaceRepository.AddAsync(space, cancellationToken)
            .ConfigureAwait(false);

        var membership = new SpaceMembership
        {
            SpaceId = createdSpace.Id,
            UserId = createdByUserId,
            Role = SpaceRole.Owner,
        };

        await _membershipRepository.AddAsync(membership, cancellationToken)
            .ConfigureAwait(false);

        return createdSpace;
    }

    /// <inheritdoc />
    public async Task<Space?> GetSpaceAsync(
        Guid spaceId,
        CancellationToken cancellationToken = default)
    {
        return await _spaceRepository.GetByIdAsync(spaceId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Space> UpdateSpaceAsync(
        Guid spaceId,
        string? name,
        SpaceType? spaceType,
        CancellationToken cancellationToken = default)
    {
        var space = await _spaceRepository.GetByIdAsync(spaceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Space), spaceId);

        if (name is not null)
        {
            var normalizedName = name.Trim().ToLowerInvariant();

            if (!string.Equals(normalizedName, space.NormalizedName, StringComparison.Ordinal))
            {
                var nameExists = await _dbContext.Set<Space>()
                    .AnyAsync(s => s.TenantId == space.TenantId
                                && s.NormalizedName == normalizedName
                                && s.Id != spaceId,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (nameExists)
                {
                    throw new DuplicateSpaceNameException(space.TenantId, normalizedName);
                }
            }

            space.Name = name.Trim();
            space.NormalizedName = normalizedName;
        }

        if (spaceType.HasValue)
        {
            space.SpaceType = spaceType.Value;
        }

        await _spaceRepository.UpdateAsync(space, cancellationToken)
            .ConfigureAwait(false);

        return space;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSpaceAsync(
        Guid spaceId,
        CancellationToken cancellationToken = default)
    {
        var space = await _spaceRepository.GetByIdAsync(spaceId, cancellationToken)
            .ConfigureAwait(false);

        if (space is null)
        {
            return false;
        }

        var memberships = await _dbContext.Set<SpaceMembership>()
            .Where(m => m.SpaceId == spaceId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var membership in memberships)
        {
            await _membershipRepository.DeleteAsync(membership.Id, cancellationToken)
                .ConfigureAwait(false);
        }

        await _spaceRepository.DeleteAsync(spaceId, cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc />
    public async Task<SpaceMembership> AddMemberAsync(
        Guid spaceId,
        Guid userId,
        SpaceRole role,
        CancellationToken cancellationToken = default)
    {
        _ = await _spaceRepository.GetByIdAsync(spaceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Space), spaceId);

        _ = await _userRepository.GetByIdAsync(userId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(User), userId);

        var membershipExists = await _dbContext.Set<SpaceMembership>()
            .AnyAsync(m => m.SpaceId == spaceId && m.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (membershipExists)
        {
            throw new DuplicateMembershipException(spaceId, userId);
        }

        var membership = new SpaceMembership
        {
            SpaceId = spaceId,
            UserId = userId,
            Role = role,
        };

        return await _membershipRepository.AddAsync(membership, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveMemberAsync(
        Guid spaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        _ = await _spaceRepository.GetByIdAsync(spaceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Space), spaceId);

        var membership = await _dbContext.Set<SpaceMembership>()
            .FirstOrDefaultAsync(m => m.SpaceId == spaceId && m.UserId == userId,
                cancellationToken)
            .ConfigureAwait(false);

        if (membership is null)
        {
            return false;
        }

        if (membership.Role == SpaceRole.Owner)
        {
            var ownerCount = await _dbContext.Set<SpaceMembership>()
                .CountAsync(m => m.SpaceId == spaceId && m.Role == SpaceRole.Owner,
                    cancellationToken)
                .ConfigureAwait(false);

            if (ownerCount <= 1)
            {
                throw new BusinessRuleViolationException(
                    "LastOwner",
                    "Cannot remove the last owner from a space. Assign another owner before removing this member.");
            }
        }

        return await _membershipRepository.DeleteAsync(membership.Id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SpaceMembership> UpdateMemberRoleAsync(
        Guid spaceId,
        Guid userId,
        SpaceRole newRole,
        CancellationToken cancellationToken = default)
    {
        _ = await _spaceRepository.GetByIdAsync(spaceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Space), spaceId);

        var membership = await _dbContext.Set<SpaceMembership>()
            .FirstOrDefaultAsync(m => m.SpaceId == spaceId && m.UserId == userId,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(SpaceMembership),
                $"SpaceId={spaceId}, UserId={userId}");

        if (membership.Role == SpaceRole.Owner && newRole != SpaceRole.Owner)
        {
            var ownerCount = await _dbContext.Set<SpaceMembership>()
                .CountAsync(m => m.SpaceId == spaceId && m.Role == SpaceRole.Owner,
                    cancellationToken)
                .ConfigureAwait(false);

            if (ownerCount <= 1)
            {
                throw new BusinessRuleViolationException(
                    "LastOwner",
                    "Cannot demote the last owner of a space. Assign another owner before changing this member's role.");
            }
        }

        membership.Role = newRole;

        await _membershipRepository.UpdateAsync(membership, cancellationToken)
            .ConfigureAwait(false);

        return membership;
    }
}
