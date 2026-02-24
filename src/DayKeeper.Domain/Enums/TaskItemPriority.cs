namespace DayKeeper.Domain.Enums;

/// <summary>
/// Indicates the priority level of a <see cref="Entities.TaskItem"/>.
/// </summary>
public enum TaskItemPriority
{
    /// <summary>No priority assigned.</summary>
    None = 0,

    /// <summary>Low priority.</summary>
    Low = 1,

    /// <summary>Medium priority.</summary>
    Medium = 2,

    /// <summary>High priority.</summary>
    High = 3,

    /// <summary>Urgent priority requiring immediate attention.</summary>
    Urgent = 4,
}
