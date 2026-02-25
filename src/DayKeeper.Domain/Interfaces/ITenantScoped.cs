namespace DayKeeper.Domain.Interfaces;

/// <summary>
/// Marker interface for entities that are scoped to a single tenant.
/// Entities implementing this interface have a required <see cref="TenantId"/>
/// and will be automatically filtered by the current tenant in EF Core queries.
/// </summary>
public interface ITenantScoped
{
    Guid TenantId { get; set; }
}
