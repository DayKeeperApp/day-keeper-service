using DayKeeper.Application.Interfaces;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// A tenant context that always returns <c>null</c>, effectively
/// bypassing tenant filtering. Used for system/admin operations,
/// background jobs, and tests that do not require tenant scoping.
/// </summary>
public sealed class NullTenantContext : ITenantContext
{
    public Guid? CurrentTenantId => null;
}
