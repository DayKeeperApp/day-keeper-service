using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Domain.Mappings;

namespace DayKeeper.Api.Tests.Unit.Mappings;

public sealed class ChangeLogEntityTypeMapTests
{
    public static TheoryData<Type, ChangeLogEntityType> AllMappings => new()
    {
        { typeof(Tenant), ChangeLogEntityType.Tenant },
        { typeof(User), ChangeLogEntityType.User },
        { typeof(Space), ChangeLogEntityType.Space },
        { typeof(SpaceMembership), ChangeLogEntityType.SpaceMembership },
        { typeof(Calendar), ChangeLogEntityType.Calendar },
        { typeof(CalendarEvent), ChangeLogEntityType.CalendarEvent },
        { typeof(EventType), ChangeLogEntityType.EventType },
        { typeof(EventReminder), ChangeLogEntityType.EventReminder },
        { typeof(TaskItem), ChangeLogEntityType.TaskItem },
        { typeof(TaskCategory), ChangeLogEntityType.TaskCategory },
        { typeof(Category), ChangeLogEntityType.Category },
        { typeof(Project), ChangeLogEntityType.Project },
        { typeof(Person), ChangeLogEntityType.Person },
        { typeof(ContactMethod), ChangeLogEntityType.ContactMethod },
        { typeof(Address), ChangeLogEntityType.Address },
        { typeof(ImportantDate), ChangeLogEntityType.ImportantDate },
        { typeof(ShoppingList), ChangeLogEntityType.ShoppingList },
        { typeof(ListItem), ChangeLogEntityType.ListItem },
        { typeof(Attachment), ChangeLogEntityType.Attachment },
        { typeof(RecurrenceException), ChangeLogEntityType.RecurrenceException },
    };

    [Theory]
    [MemberData(nameof(AllMappings))]
    public void GetEntityType_ReturnsCorrectMapping(Type clrType, ChangeLogEntityType expected)
    {
        ChangeLogEntityTypeMap.GetEntityType(clrType).Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(AllMappings))]
    public void TryGetEntityType_ReturnsTrue_ForMappedType(
        Type clrType, ChangeLogEntityType expected)
    {
        ChangeLogEntityTypeMap.TryGetEntityType(clrType, out var result).Should().BeTrue();
        result.Should().Be(expected);
    }

    [Fact]
    public void GetEntityType_ThrowsArgumentException_ForChangeLog()
    {
        var act = () => ChangeLogEntityTypeMap.GetEntityType(typeof(ChangeLog));

        act.Should().Throw<ArgumentException>()
            .WithParameterName("clrType");
    }

    [Fact]
    public void TryGetEntityType_ReturnsFalse_ForUnmappedType()
    {
        ChangeLogEntityTypeMap.TryGetEntityType(typeof(ChangeLog), out _).Should().BeFalse();
    }

    [Fact]
    public void AllEnumValues_HaveCorrespondingClrMapping()
    {
        var allEnumValues = Enum.GetValues<ChangeLogEntityType>();

        foreach (var value in allEnumValues)
        {
            AllMappings
                .Any(row => (ChangeLogEntityType)row[1] == value)
                .Should().BeTrue(
                    $"ChangeLogEntityType.{value} should have a CLR type mapping");
        }
    }

    // ── Reverse map (enum → CLR type) ────────────────────────────────

    [Theory]
    [MemberData(nameof(AllMappings))]
    public void GetClrType_ReturnsCorrectMapping(
        Type expectedClrType, ChangeLogEntityType entityType)
    {
        ChangeLogEntityTypeMap.GetClrType(entityType).Should().Be(expectedClrType);
    }

    [Theory]
    [MemberData(nameof(AllMappings))]
    public void TryGetClrType_ReturnsTrue_ForMappedEnum(
        Type expectedClrType, ChangeLogEntityType entityType)
    {
        ChangeLogEntityTypeMap.TryGetClrType(entityType, out var result).Should().BeTrue();
        result.Should().Be(expectedClrType);
    }

    [Fact]
    public void GetClrType_ThrowsArgumentException_ForUnmappedValue()
    {
        var act = () => ChangeLogEntityTypeMap.GetClrType((ChangeLogEntityType)999);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("entityType");
    }

    [Fact]
    public void TryGetClrType_ReturnsFalse_ForUnmappedValue()
    {
        ChangeLogEntityTypeMap.TryGetClrType((ChangeLogEntityType)999, out _)
            .Should().BeFalse();
    }
}
