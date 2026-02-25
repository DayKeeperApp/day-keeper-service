using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="IUserService"/>.
/// Orchestrates business rules for user management within a tenant
/// using repository abstractions and direct DbContext queries.
/// </summary>
public sealed class UserService(
    IRepository<User> userRepository,
    IRepository<Tenant> tenantRepository,
    DbContext dbContext) : IUserService
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IRepository<Tenant> _tenantRepository = tenantRepository;
    private readonly DbContext _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<User> CreateUserAsync(
        Guid tenantId,
        string displayName,
        string email,
        string timezone,
        WeekStart weekStart,
        string? locale = null,
        CancellationToken cancellationToken = default)
    {
        _ = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Tenant), tenantId);

        var normalizedEmail = email.Trim().ToLowerInvariant();

        var emailExists = await _dbContext.Set<User>()
            .AnyAsync(u => u.TenantId == tenantId && u.Email == normalizedEmail,
                cancellationToken)
            .ConfigureAwait(false);

        if (emailExists)
        {
            throw new DuplicateEmailException(tenantId, normalizedEmail);
        }

        var user = new User
        {
            TenantId = tenantId,
            DisplayName = displayName.Trim(),
            Email = normalizedEmail,
            Timezone = timezone.Trim(),
            WeekStart = weekStart,
            Locale = locale?.Trim(),
        };

        return await _userRepository.AddAsync(user, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<User?> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetByIdAsync(userId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByEmailAsync(
        Guid tenantId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == normalizedEmail,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetUsersByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<User>()
            .Where(u => u.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<User> UpdateUserAsync(
        Guid userId,
        string? displayName,
        string? email,
        string? timezone,
        WeekStart? weekStart,
        string? locale,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(User), userId);

        if (displayName is not null)
        {
            user.DisplayName = displayName.Trim();
        }

        if (email is not null)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            if (!string.Equals(normalizedEmail, user.Email, StringComparison.Ordinal))
            {
                var emailExists = await _dbContext.Set<User>()
                    .AnyAsync(u => u.TenantId == user.TenantId
                                && u.Email == normalizedEmail
                                && u.Id != userId,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (emailExists)
                {
                    throw new DuplicateEmailException(user.TenantId, normalizedEmail);
                }

                user.Email = normalizedEmail;
            }
        }

        if (timezone is not null)
        {
            user.Timezone = timezone.Trim();
        }

        if (weekStart.HasValue)
        {
            user.WeekStart = weekStart.Value;
        }

        if (locale is not null)
        {
            user.Locale = locale.Trim().Length > 0 ? locale.Trim() : null;
        }

        await _userRepository.UpdateAsync(user, cancellationToken)
            .ConfigureAwait(false);

        return user;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _userRepository.DeleteAsync(userId, cancellationToken)
            .ConfigureAwait(false);
    }
}
