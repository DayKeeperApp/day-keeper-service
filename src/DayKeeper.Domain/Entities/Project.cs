namespace DayKeeper.Domain.Entities;

/// <summary>
/// An organizational container for <see cref="TaskItem"/> entities within a <see cref="Space"/>.
/// Tasks may optionally belong to a project.
/// </summary>
public class Project : BaseEntity
{
    /// <summary>Foreign key to the owning <see cref="Space"/>.</summary>
    public Guid SpaceId { get; set; }

    /// <summary>Display name for the project.</summary>
    public required string Name { get; set; }

    /// <summary>Lowercased, trimmed name used for uniqueness checks and lookups.</summary>
    public required string NormalizedName { get; set; }

    /// <summary>
    /// Optional description providing additional context about the project.
    /// <c>null</c> if no description has been provided.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>Navigation to the owning space.</summary>
    public Space Space { get; set; } = null!;

    /// <summary>Tasks belonging to this project.</summary>
    public ICollection<TaskItem> TaskItems { get; set; } = [];
}
