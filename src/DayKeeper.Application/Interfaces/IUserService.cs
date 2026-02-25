using DayKeeper.Application.Exceptions;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Application service for managing users within a tenant.
/// Orchestrates business rules, validation, and persistence for
/// <see cref="User"/> entities.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Creates a new user within the specified tenant.
    /// </summary>
    /// <param name="tenantId">The tenant under which to create the user.</param>
    /// <param name="displayName">The user's display name.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="timezone">The user's IANA timezone identifier.</param>
    /// <param name="weekStart">The user's preferred first day of the week.</param>
    /// <param name="locale">Optional locale string for formatting (e.g. "en-US").</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The newly created user.</returns>
    /// <exception cref="EntityNotFoundException">The specified tenant does not exist.</exception>
    /// <exception cref="DuplicateEmailException">A user with the same email already exists in this tenant.</exception>
    Task<User> CreateUserAsync(
        Guid tenantId,
        string displayName,
        string email,
        string timezone,
        WeekStart weekStart,
        string? locale = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by its unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The user if found; otherwise, <c>null</c>.</returns>
    Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by email within a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant to search within.</param>
    /// <param name="email">The email to look up (will be normalized).</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The user if found; otherwise, <c>null</c>.</returns>
    Task<User?> GetUserByEmailAsync(
        Guid tenantId,
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all users belonging to the specified tenant.
    /// </summary>
    /// <param name="tenantId">The tenant whose users to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A read-only list of users in the tenant.</returns>
    Task<IReadOnlyList<User>> GetUsersByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates properties of an existing user. Pass <c>null</c> for any property to leave it unchanged.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to update.</param>
    /// <param name="displayName">The new display name, or <c>null</c> to leave unchanged.</param>
    /// <param name="email">The new email, or <c>null</c> to leave unchanged.</param>
    /// <param name="timezone">The new timezone, or <c>null</c> to leave unchanged.</param>
    /// <param name="weekStart">The new week start, or <c>null</c> to leave unchanged.</param>
    /// <param name="locale">The new locale, or <c>null</c> to leave unchanged. Pass empty string to clear.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The updated user.</returns>
    /// <exception cref="EntityNotFoundException">The user does not exist.</exception>
    /// <exception cref="DuplicateEmailException">The new email conflicts with an existing user in the same tenant.</exception>
    Task<User> UpdateUserAsync(
        Guid userId,
        string? displayName,
        string? email,
        string? timezone,
        WeekStart? weekStart,
        string? locale,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a user. Child entities are cascade-deleted at the database level.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the user was found and deleted; <c>false</c> if not found.</returns>
    Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
