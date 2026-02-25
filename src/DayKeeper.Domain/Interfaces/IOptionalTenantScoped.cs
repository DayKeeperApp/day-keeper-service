namespace DayKeeper.Domain.Interfaces;

/// <summary>
/// Marker interface for entities that may be scoped to a tenant or system-defined.
/// Entities implementing this interface have a nullable <see cref="TenantId"/>.
/// System-defined records (<c>TenantId == null</c>) are visible to all tenants.
/// </summary>
public interface IOptionalTenantScoped
{
    Guid? TenantId { get; set; }
}
