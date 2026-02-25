namespace DayKeeper.Application.Exceptions;

/// <summary>
/// Thrown when attempting to create or update a user with an email
/// that already exists within the same tenant.
/// </summary>
public sealed class DuplicateEmailException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateEmailException"/> class.
    /// </summary>
    /// <param name="tenantId">The tenant in which the duplicate was detected.</param>
    /// <param name="email">The email that collides.</param>
    public DuplicateEmailException(Guid tenantId, string email)
        : base($"A user with the email '{email}' already exists in tenant '{tenantId}'.")
    {
        TenantId = tenantId;
        Email = email;
    }

    /// <summary>The tenant in which the duplicate was detected.</summary>
    public Guid TenantId { get; }

    /// <summary>The email that collides.</summary>
    public string Email { get; }
}
