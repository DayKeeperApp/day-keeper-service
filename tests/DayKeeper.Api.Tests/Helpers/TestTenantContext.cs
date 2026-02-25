using DayKeeper.Application.Interfaces;

namespace DayKeeper.Api.Tests.Helpers;

/// <summary>
/// Test implementation of <see cref="ITenantContext"/> that allows
/// the tenant ID to be set explicitly for testing purposes.
/// </summary>
public sealed class TestTenantContext : ITenantContext
{
    public Guid? CurrentTenantId { get; set; }
}
