using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;

namespace DayKeeper.Api.GraphQL.Mutations;

/// <summary>
/// Mutation resolvers for <see cref="ImportantDate"/> operations.
/// </summary>
[ExtendObjectType(typeof(Mutation))]
public sealed class ImportantDateMutations
{
    /// <summary>Creates a new important date for a person.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    public Task<ImportantDate> CreateImportantDateAsync(
        Guid personId,
        string label,
        DateOnly dateValue,
        Guid? eventTypeId,
        IPersonService personService,
        CancellationToken cancellationToken)
    {
        return personService.CreateImportantDateAsync(
            personId, label, dateValue, eventTypeId, cancellationToken);
    }

    /// <summary>Updates an existing important date.</summary>
    [Error<InputValidationException>]
    [Error<EntityNotFoundException>]
    public Task<ImportantDate> UpdateImportantDateAsync(
        Guid id,
        string? label,
        DateOnly? dateValue,
        Guid? eventTypeId,
        IPersonService personService,
        CancellationToken cancellationToken)
    {
        return personService.UpdateImportantDateAsync(
            id, label, dateValue, eventTypeId, cancellationToken);
    }

    /// <summary>Soft-deletes an important date.</summary>
    public Task<bool> DeleteImportantDateAsync(
        Guid id,
        IPersonService personService,
        CancellationToken cancellationToken)
    {
        return personService.DeleteImportantDateAsync(id, cancellationToken);
    }
}
