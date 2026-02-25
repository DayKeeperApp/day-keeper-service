using DayKeeper.Application.Exceptions;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Application service for checking user permissions within a space.
/// Provides non-throwing query methods for authorization checks and
/// a throwing enforcement method for guarding mutations.
/// </summary>
public interface ISpaceAuthorizationService
{
    /// <summary>
    /// Retrieves the role a user holds in a space.
    /// </summary>
    /// <param name="spaceId">The unique identifier of the space.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The user's <see cref="SpaceRole"/> if they are a member; otherwise, <c>null</c>.</returns>
    Task<SpaceRole?> GetUserRoleAsync(
        Guid spaceId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a user has at least the specified role in a space.
    /// </summary>
    /// <param name="spaceId">The unique identifier of the space.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="minimumRole">The minimum <see cref="SpaceRole"/> required.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the user is a member with a role &gt;= <paramref name="minimumRole"/>; otherwise, <c>false</c>.</returns>
    Task<bool> HasMinimumRoleAsync(
        Guid spaceId,
        Guid userId,
        SpaceRole minimumRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a user can view the space (has at least <see cref="SpaceRole.Viewer"/> role).
    /// </summary>
    /// <param name="spaceId">The unique identifier of the space.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the user is a member of the space; otherwise, <c>false</c>.</returns>
    Task<bool> CanViewAsync(
        Guid spaceId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a user can edit the space (has at least <see cref="SpaceRole.Editor"/> role).
    /// </summary>
    /// <param name="spaceId">The unique identifier of the space.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the user has Editor or Owner role; otherwise, <c>false</c>.</returns>
    Task<bool> CanEditAsync(
        Guid spaceId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a user is an owner of the space.
    /// </summary>
    /// <param name="spaceId">The unique identifier of the space.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the user has Owner role; otherwise, <c>false</c>.</returns>
    Task<bool> IsOwnerAsync(
        Guid spaceId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a user has at least the specified role in a space, throwing if they do not.
    /// </summary>
    /// <param name="spaceId">The unique identifier of the space.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="minimumRole">The minimum <see cref="SpaceRole"/> required.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <exception cref="BusinessRuleViolationException">
    /// The user is not a member of the space or does not have the required role.
    /// </exception>
    Task EnsureMinimumRoleAsync(
        Guid spaceId,
        Guid userId,
        SpaceRole minimumRole,
        CancellationToken cancellationToken = default);
}
