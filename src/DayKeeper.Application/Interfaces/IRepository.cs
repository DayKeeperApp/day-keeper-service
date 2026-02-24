using DayKeeper.Domain.Entities;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Generic repository abstraction for CRUD operations on domain entities.
/// All query operations automatically exclude soft-deleted entities.
/// </summary>
/// <typeparam name="T">The entity type, which must derive from <see cref="BaseEntity"/>.</typeparam>
public interface IRepository<T>
    where T : BaseEntity
{
    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// Returns <c>null</c> if no matching entity exists or the entity has been soft-deleted.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The entity if found; otherwise, <c>null</c>.</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all non-deleted entities of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A read-only list of all matching entities.</returns>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the data store and persists the change.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The tracked entity after persistence.</returns>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the data store and persists the change.
    /// </summary>
    /// <param name="entity">The entity with updated values.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes an entity by setting its <see cref="BaseEntity.DeletedAt"/> timestamp.
    /// The entity remains in the data store but is excluded from future queries.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to soft-delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the entity was found and soft-deleted; <c>false</c> if no matching entity exists.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
