namespace DayKeeper.Application.Exceptions;

/// <summary>
/// Thrown when attempting to create or rename a tenant to a slug
/// that already exists.
/// </summary>
public sealed class DuplicateSlugException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateSlugException"/> class.
    /// </summary>
    /// <param name="slug">The slug that collides.</param>
    public DuplicateSlugException(string slug)
        : base($"A tenant with the slug '{slug}' already exists.")
    {
        Slug = slug;
    }

    /// <summary>The slug that collides.</summary>
    public string Slug { get; }
}
