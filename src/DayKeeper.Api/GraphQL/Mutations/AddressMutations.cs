using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="Address"/> operations.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class AddressMutations
{
    /// <summary>Creates a new address for a person.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    public Task<Address> CreateAddressAsync(
        Guid personId,
        string? label,
        string street1,
        string? street2,
        string city,
        string? state,
        string? postalCode,
        string country,
        bool isPrimary,
        IPersonService personService,
        CancellationToken cancellationToken)
    {
        return personService.CreateAddressAsync(
            personId, label, street1, street2, city, state, postalCode, country, isPrimary,
            cancellationToken);
    }

    /// <summary>Updates an existing address.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    public Task<Address> UpdateAddressAsync(
        Guid id,
        string? label,
        string? street1,
        string? street2,
        string? city,
        string? state,
        string? postalCode,
        string? country,
        bool? isPrimary,
        IPersonService personService,
        CancellationToken cancellationToken)
    {
        return personService.UpdateAddressAsync(
            id, label, street1, street2, city, state, postalCode, country, isPrimary,
            cancellationToken);
    }

    /// <summary>Soft-deletes an address.</summary>
    public Task<bool> DeleteAddressAsync(
        Guid id,
        IPersonService personService,
        CancellationToken cancellationToken)
    {
        return personService.DeleteAddressAsync(id, cancellationToken);
    }
}
