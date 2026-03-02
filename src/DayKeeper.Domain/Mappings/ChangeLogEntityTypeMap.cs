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
    private static readonly FrozenDictionary<Type, ChangeLogEntityType> _typeToEnum =
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
            [typeof(Device)] = ChangeLogEntityType.Device,
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<ChangeLogEntityType, Type> _enumToType =
        _typeToEnum
            .ToDictionary(kvp => kvp.Value, kvp => kvp.Key)
            .ToFrozenDictionary();

    /// <summary>
    /// Attempts to resolve the <see cref="ChangeLogEntityType"/> for the given CLR type.
    /// Returns <c>false</c> for unmapped types (e.g. <see cref="ChangeLog"/>).
    /// </summary>
    public static bool TryGetEntityType(Type clrType, out ChangeLogEntityType entityType)
        => _typeToEnum.TryGetValue(clrType, out entityType);

    /// <summary>
    /// Resolves the <see cref="ChangeLogEntityType"/> for the given CLR type.
    /// Throws <see cref="ArgumentException"/> if the type is not mapped.
    /// </summary>
    public static ChangeLogEntityType GetEntityType(Type clrType)
        => _typeToEnum.TryGetValue(clrType, out var entityType)
            ? entityType
            : throw new ArgumentException(
                $"No ChangeLogEntityType mapping for {clrType.Name}.", nameof(clrType));

    /// <summary>
    /// Attempts to resolve the CLR type for the given <see cref="ChangeLogEntityType"/>.
    /// Returns <c>false</c> if the enum value is not mapped.
    /// </summary>
    public static bool TryGetClrType(ChangeLogEntityType entityType, out Type clrType)
        => _enumToType.TryGetValue(entityType, out clrType!);

    /// <summary>
    /// Resolves the CLR type for the given <see cref="ChangeLogEntityType"/>.
    /// Throws <see cref="ArgumentException"/> if the value is not mapped.
    /// </summary>
    public static Type GetClrType(ChangeLogEntityType entityType)
        => _enumToType.TryGetValue(entityType, out var clrType)
            ? clrType
            : throw new ArgumentException(
                $"No CLR type mapping for {entityType}.", nameof(entityType));
}
