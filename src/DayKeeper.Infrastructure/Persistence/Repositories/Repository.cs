using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic EF Core repository implementation that provides CRUD operations
/// with automatic soft-delete support for <see cref="BaseEntity"/>-derived entities.
/// </summary>
/// <typeparam name="T">The entity type, which must derive from <see cref="BaseEntity"/>.</typeparam>
/// <param name="context">The EF Core database context.</param>
/// <param name="dateTimeProvider">Provider for the current UTC timestamp, used for soft-delete.</param>
public sealed class Repository<T>(
    DbContext context,
    IDateTimeProvider dateTimeProvider) : IRepository<T>
    where T : BaseEntity
{
    private readonly DbContext _context = context;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    /// <inheritdoc />
    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Set<T>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Set<T>().ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        _context.Set<T>().Add(entity);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Set<T>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return false;
        }

        entity.DeletedAt = _dateTimeProvider.UtcNow;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
