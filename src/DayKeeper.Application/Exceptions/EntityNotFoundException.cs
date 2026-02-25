namespace DayKeeper.Application.Exceptions;

/// <summary>
/// Thrown when a required entity cannot be found.
/// </summary>
public sealed class EntityNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance for a single-key entity lookup.
    /// </summary>
    /// <param name="entityName">The type name of the entity that was not found.</param>
    /// <param name="entityId">The identifier that was searched for.</param>
    public EntityNotFoundException(string entityName, Guid entityId)
        : base($"{entityName} with id '{entityId}' was not found.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }

    /// <summary>
    /// Initializes a new instance for a composite-key or descriptive lookup.
    /// </summary>
    /// <param name="entityName">The type name of the entity that was not found.</param>
    /// <param name="description">A description of the lookup criteria.</param>
    public EntityNotFoundException(string entityName, string description)
        : base($"{entityName} was not found: {description}.")
    {
        EntityName = entityName;
    }

    /// <summary>The type name of the entity that was not found.</summary>
    public string EntityName { get; }

    /// <summary>The identifier that was searched for, or <see cref="Guid.Empty"/> for composite-key lookups.</summary>
    public Guid EntityId { get; }
}
