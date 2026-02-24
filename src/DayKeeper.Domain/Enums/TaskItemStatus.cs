namespace DayKeeper.Domain.Enums;

/// <summary>
/// Tracks the lifecycle state of a <see cref="Entities.TaskItem"/>.
/// </summary>
public enum TaskItemStatus
{
    /// <summary>The task is open and not yet started.</summary>
    Open = 0,

    /// <summary>The task is actively being worked on.</summary>
    InProgress = 1,

    /// <summary>The task has been completed.</summary>
    Completed = 2,

    /// <summary>The task has been cancelled and will not be completed.</summary>
    Cancelled = 3,
}
