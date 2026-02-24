namespace DayKeeper.Domain.Entities;

/// <summary>
/// Associates a <see cref="TaskItem"/> with a <see cref="Category"/>.
/// </summary>
public class TaskCategory : BaseEntity
{
    /// <summary>Foreign key to the <see cref="TaskItem"/>.</summary>
    public Guid TaskItemId { get; set; }

    /// <summary>Foreign key to the <see cref="Category"/>.</summary>
    public Guid CategoryId { get; set; }

    /// <summary>Navigation to the task.</summary>
    public TaskItem TaskItem { get; set; } = null!;

    /// <summary>Navigation to the category.</summary>
    public Category Category { get; set; } = null!;
}
