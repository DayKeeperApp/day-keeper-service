using System.Collections.Frozen;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Domain.Mappings;

/// <summary>
/// Provides a bidirectional mapping between CLR entity types and
/// <see cref="ChangeLogEntityType"/> enum values for change-log tracking.
/// </summary>
public static class ChangeLogEntityTypeMap
{
    private static readonly FrozenDictionary<Type, ChangeLogEntityType> TypeToEnum =
        new Dictionary<Type, ChangeLogEntityType>
        {
            [typeof(Tenant)] = ChangeLogEntityType.Tenant,
            [typeof(User)] = ChangeLogEntityType.User,
            [typeof(Space)] = ChangeLogEntityType.Space,
            [typeof(SpaceMembership)] = ChangeLogEntityType.SpaceMembership,
            [typeof(Calendar)] = ChangeLogEntityType.Calendar,
            [typeof(CalendarEvent)] = ChangeLogEntityType.CalendarEvent,
            [typeof(EventType)] = ChangeLogEntityType.EventType,
            [typeof(EventReminder)] = ChangeLogEntityType.EventReminder,
            [typeof(TaskItem)] = ChangeLogEntityType.TaskItem,
            [typeof(TaskCategory)] = ChangeLogEntityType.TaskCategory,
            [typeof(Category)] = ChangeLogEntityType.Category,
            [typeof(Project)] = ChangeLogEntityType.Project,
            [typeof(Person)] = ChangeLogEntityType.Person,
            [typeof(ContactMethod)] = ChangeLogEntityType.ContactMethod,
            [typeof(Address)] = ChangeLogEntityType.Address,
            [typeof(ImportantDate)] = ChangeLogEntityType.ImportantDate,
            [typeof(ShoppingList)] = ChangeLogEntityType.ShoppingList,
            [typeof(ListItem)] = ChangeLogEntityType.ListItem,
            [typeof(Attachment)] = ChangeLogEntityType.Attachment,
            [typeof(RecurrenceException)] = ChangeLogEntityType.RecurrenceException,
        }.ToFrozenDictionary();

    /// <summary>
    /// Attempts to resolve the <see cref="ChangeLogEntityType"/> for the given CLR type.
    /// Returns <c>false</c> for unmapped types (e.g. <see cref="ChangeLog"/>).
    /// </summary>
    public static bool TryGetEntityType(Type clrType, out ChangeLogEntityType entityType)
        => TypeToEnum.TryGetValue(clrType, out entityType);

    /// <summary>
    /// Resolves the <see cref="ChangeLogEntityType"/> for the given CLR type.
    /// Throws <see cref="ArgumentException"/> if the type is not mapped.
    /// </summary>
    public static ChangeLogEntityType GetEntityType(Type clrType)
        => TypeToEnum.TryGetValue(clrType, out var entityType)
            ? entityType
            : throw new ArgumentException(
                $"No ChangeLogEntityType mapping for {clrType.Name}.", nameof(clrType));
}
