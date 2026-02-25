namespace DayKeeper.Domain.Enums;

/// <summary>
/// Identifies the type of domain entity recorded in a <see cref="Entities.ChangeLog"/> entry.
/// </summary>
public enum ChangeLogEntityType
{
    /// <summary>A <see cref="Entities.Tenant"/> entity.</summary>
    Tenant = 0,

    /// <summary>A <see cref="Entities.User"/> entity.</summary>
    User = 1,

    /// <summary>A <see cref="Entities.Space"/> entity.</summary>
    Space = 2,

    /// <summary>A <see cref="Entities.SpaceMembership"/> entity.</summary>
    SpaceMembership = 3,

    /// <summary>A <see cref="Entities.Calendar"/> entity.</summary>
    Calendar = 4,

    /// <summary>A <see cref="Entities.CalendarEvent"/> entity.</summary>
    CalendarEvent = 5,

    /// <summary>A <see cref="Entities.EventType"/> entity.</summary>
    EventType = 6,

    /// <summary>A <see cref="Entities.EventReminder"/> entity.</summary>
    EventReminder = 7,

    /// <summary>A <see cref="Entities.TaskItem"/> entity.</summary>
    TaskItem = 8,

    /// <summary>A <see cref="Entities.TaskCategory"/> entity.</summary>
    TaskCategory = 9,

    /// <summary>A <see cref="Entities.Category"/> entity.</summary>
    Category = 10,

    /// <summary>A <see cref="Entities.Project"/> entity.</summary>
    Project = 11,

    /// <summary>A <see cref="Entities.Person"/> entity.</summary>
    Person = 12,

    /// <summary>A <see cref="Entities.ContactMethod"/> entity.</summary>
    ContactMethod = 13,

    /// <summary>A <see cref="Entities.Address"/> entity.</summary>
    Address = 14,

    /// <summary>A <see cref="Entities.ImportantDate"/> entity.</summary>
    ImportantDate = 15,

    /// <summary>A <see cref="Entities.ShoppingList"/> entity.</summary>
    ShoppingList = 16,

    /// <summary>A <see cref="Entities.ListItem"/> entity.</summary>
    ListItem = 17,

    /// <summary>A <see cref="Entities.Attachment"/> entity.</summary>
    Attachment = 18,

    /// <summary>A <see cref="Entities.RecurrenceException"/> entity.</summary>
    RecurrenceException = 19,
}
