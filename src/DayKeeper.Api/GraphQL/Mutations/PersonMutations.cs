using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="Person"/> entities.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class PersonMutations
{
    /// <summary>Creates a new person within a space.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<DuplicatePersonNameException>]
    public Task<Person> CreatePersonAsync(
        Guid spaceId,
        string firstName,
        string lastName,
        string? notes,
        IPersonService personService,
        CancellationToken cancellationToken)
    {
        return personService.CreatePersonAsync(
            spaceId, firstName, lastName, notes, cancellationToken);
    }

    /// <summary>Updates an existing person.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    [Error<DuplicatePersonNameException>]
    public Task<Person> UpdatePersonAsync(
        Guid id,
        string? firstName,
        string? lastName,
        string? notes,
        IPersonService personService,
        CancellationToken cancellationToken)
    {
        return personService.UpdatePersonAsync(
            id, firstName, lastName, notes, cancellationToken);
    }

    /// <summary>Soft-deletes a person and all associated child entities.</summary>
    public Task<bool> DeletePersonAsync(
        Guid id,
        IPersonService personService,
        CancellationToken cancellationToken)
    {
        return personService.DeletePersonAsync(id, cancellationToken);
    }
}
