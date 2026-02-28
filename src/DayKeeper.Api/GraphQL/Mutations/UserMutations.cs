using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="User"/> entities.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class UserMutations
{
    /// <summary>Creates a new user within a tenant.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<DuplicateEmailException>]
    public Task<User> CreateUserAsync(
        Guid tenantId,
        string displayName,
        string email,
        string timezone,
        WeekStart weekStart,
        string? locale,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        return userService.CreateUserAsync(
            tenantId, displayName, email, timezone, weekStart, locale, cancellationToken);
    }

    /// <summary>Updates an existing user.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<DuplicateEmailException>]
    public Task<User> UpdateUserAsync(
        Guid id,
        string? displayName,
        string? email,
        string? timezone,
        WeekStart? weekStart,
        string? locale,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        return userService.UpdateUserAsync(
            id, displayName, email, timezone, weekStart, locale, cancellationToken);
    }

    /// <summary>Soft-deletes a user.</summary>
    public Task<bool> DeleteUserAsync(
        Guid id,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        return userService.DeleteUserAsync(id, cancellationToken);
    }
}
