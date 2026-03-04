namespace DayKeeper.Application.Exceptions;

/// <summary>
/// Thrown when attempting to create or rename a space to a name
/// that already exists within the same tenant.
/// </summary>
public sealed class DuplicateSpaceNameException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateSpaceNameException"/> class.
    /// </summary>
    /// <param name="tenantId">The tenant in which the duplicate was detected, or <c>null</c> for system spaces.</param>
    /// <param name="normalizedName">The normalized name that collides.</param>
    public DuplicateSpaceNameException(Guid? tenantId, string normalizedName)
        : base($"A space with the name '{normalizedName}' already exists in tenant '{tenantId?.ToString() ?? "system"}'.")
    {
        TenantId = tenantId;
        NormalizedName = normalizedName;
    }

    /// <summary>The tenant in which the duplicate was detected, or <c>null</c> for system spaces.</summary>
    public Guid? TenantId { get; }

    /// <summary>The normalized name that collides.</summary>
    public string NormalizedName { get; }
}
