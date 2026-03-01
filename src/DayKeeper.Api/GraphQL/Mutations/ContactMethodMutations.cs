using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="ContactMethod"/> operations.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class ContactMethodMutations
{
    /// <summary>Creates a new contact method for a person.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    public Task<ContactMethod> CreateContactMethodAsync(
        Guid personId,
        ContactMethodType type,
        string value,
        string? label,
        bool isPrimary,
        IPersonService personService,
        CancellationToken cancellationToken)
    {
        return personService.CreateContactMethodAsync(
            personId, type, value, label, isPrimary, cancellationToken);
    }

    /// <summary>Updates an existing contact method.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    public Task<ContactMethod> UpdateContactMethodAsync(
        Guid id,
        ContactMethodType? type,
        string? value,
        string? label,
        bool? isPrimary,
        IPersonService personService,
        CancellationToken cancellationToken)
    {
        return personService.UpdateContactMethodAsync(
            id, type, value, label, isPrimary, cancellationToken);
    }

    /// <summary>Soft-deletes a contact method.</summary>
    public Task<bool> DeleteContactMethodAsync(
        Guid id,
        IPersonService personService,
        CancellationToken cancellationToken)
    {
        return personService.DeleteContactMethodAsync(id, cancellationToken);
    }
}
