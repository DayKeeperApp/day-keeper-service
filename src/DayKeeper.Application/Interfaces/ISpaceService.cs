using DayKeeper.Application.Exceptions;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Application service for managing spaces and space memberships.
/// Orchestrates business rules, validation, and persistence for
/// <see cref="Space"/> and <see cref="SpaceMembership"/> entities.
/// </summary>
public interface ISpaceService
{
    /// <summary>
    /// Creates a new space within the specified tenant.
    /// The caller who creates the space is automatically added as an <see cref="SpaceRole.Owner"/>.
    /// </summary>
    /// <param name="tenantId">The tenant under which to create the space.</param>
    /// <param name="name">The display name for the space.</param>
    /// <param name="spaceType">The type of space to create.</param>
    /// <param name="createdByUserId">The user creating the space, who becomes the initial owner.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The newly created space.</returns>
    /// <exception cref="EntityNotFoundException">The specified user does not exist.</exception>
    /// <exception cref="DuplicateSpaceNameException">A space with the same normalized name already exists in this tenant.</exception>
    Task<Space> CreateSpaceAsync(
        Guid tenantId,
        string name,
        SpaceType spaceType,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a space by its unique identifier.
    /// </summary>
    /// <param name="spaceId">The unique identifier of the space.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The space if found; otherwise, <c>null</c>.</returns>
    Task<Space?> GetSpaceAsync(Guid spaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the name and/or type of an existing space.
    /// </summary>
    /// <param name="spaceId">The unique identifier of the space to update.</param>
    /// <param name="name">The new display name, or <c>null</c> to leave unchanged.</param>
    /// <param name="spaceType">The new space type, or <c>null</c> to leave unchanged.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The updated space.</returns>
    /// <exception cref="EntityNotFoundException">The space does not exist.</exception>
    /// <exception cref="DuplicateSpaceNameException">The new name conflicts with an existing space in the same tenant.</exception>
    Task<Space> UpdateSpaceAsync(
        Guid spaceId,
        string? name,
        SpaceType? spaceType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a space and all its memberships.
    /// </summary>
    /// <param name="spaceId">The unique identifier of the space to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the space was found and deleted; <c>false</c> if not found.</returns>
    Task<bool> DeleteSpaceAsync(Guid spaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a user as a member of a space with the specified role.
    /// </summary>
    /// <param name="spaceId">The space to add the member to.</param>
    /// <param name="userId">The user to add.</param>
    /// <param name="role">The role to assign.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The newly created membership.</returns>
    /// <exception cref="EntityNotFoundException">The space or user does not exist.</exception>
    /// <exception cref="DuplicateMembershipException">The user is already a member of this space.</exception>
    Task<SpaceMembership> AddMemberAsync(
        Guid spaceId,
        Guid userId,
        SpaceRole role,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a user's membership from a space.
    /// </summary>
    /// <param name="spaceId">The space to remove the member from.</param>
    /// <param name="userId">The user to remove.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the membership was found and removed; <c>false</c> if not found.</returns>
    /// <exception cref="EntityNotFoundException">The space does not exist.</exception>
    /// <exception cref="BusinessRuleViolationException">Removing this member would leave the space with no owners.</exception>
    Task<bool> RemoveMemberAsync(
        Guid spaceId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the role of an existing space member.
    /// </summary>
    /// <param name="spaceId">The space containing the membership.</param>
    /// <param name="userId">The user whose role to update.</param>
    /// <param name="newRole">The new role to assign.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The updated membership.</returns>
    /// <exception cref="EntityNotFoundException">The space does not exist or the user is not a member.</exception>
    /// <exception cref="BusinessRuleViolationException">Demoting the last owner would leave the space with no owners.</exception>
    Task<SpaceMembership> UpdateMemberRoleAsync(
        Guid spaceId,
        Guid userId,
        SpaceRole newRole,
        CancellationToken cancellationToken = default);
}
