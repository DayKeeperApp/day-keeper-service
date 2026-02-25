namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Provides the tenant identity for the current execution scope.
/// Used by EF Core global query filters to automatically scope queries
/// to the current tenant.
/// </summary>
/// <remarks>
/// When <see cref="CurrentTenantId"/> is <c>null</c>, tenant filtering
/// is bypassed (e.g., for system/admin operations).
/// Implementations should be registered as Scoped services.
/// </remarks>
public interface ITenantContext
{
    /// <summary>
    /// The unique identifier of the current tenant, or <c>null</c>
    /// if no tenant context is established (system/admin scope).
    /// </summary>
    Guid? CurrentTenantId { get; }
}
