using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Services;

namespace DayKeeper.Api.Tests.Unit.Services;

public sealed class SyncSerializerTests
{
    private readonly SyncSerializer _sut = new();

    // ── Navigation exclusion ──────────────────────────────────────────

    [Fact]
    public void Serialize_ExcludesReferenceNavigationProperties()
    {
        var calendarEvent = CreateCalendarEvent();
        calendarEvent.Calendar = new Calendar
        {
            Id = calendarEvent.CalendarId,
            SpaceId = Guid.NewGuid(),
            Name = "Nav Calendar",
            NormalizedName = "nav calendar",
            Color = "#0000FF",
        };

        var json = _sut.Serialize(calendarEvent);

        json.TryGetProperty("calendar", out _).Should().BeFalse();
        json.TryGetProperty("eventType", out _).Should().BeFalse();
    }

    [Fact]
    public void Serialize_ExcludesCollectionNavigationProperties()
    {
        var calendar = new Calendar
        {
            SpaceId = Guid.NewGuid(),
            Name = "Test",
            NormalizedName = "test",
            Color = "#FF0000",
        };

        var json = _sut.Serialize(calendar);

        json.TryGetProperty("events", out _).Should().BeFalse();
    }

    [Fact]
    public void Serialize_ExcludesIsDeletedProperty()
    {
        var user = CreateUser();

        var json = _sut.Serialize(user);

        json.TryGetProperty("isDeleted", out _).Should().BeFalse();
    }

    [Fact]
    public void Serialize_ExcludesIsSystemProperty()
    {
        var eventType = new EventType
        {
            TenantId = null,
            Name = "System Type",
            NormalizedName = "system type",
            Color = "#FF0000",
        };

        var json = _sut.Serialize(eventType);

        json.TryGetProperty("isSystem", out _).Should().BeFalse();
    }

    // ── Scalar inclusion ──────────────────────────────────────────────

    [Fact]
    public void Serialize_IncludesForeignKeyScalars()
    {
        var calendarEvent = CreateCalendarEvent();

        var json = _sut.Serialize(calendarEvent);

        json.GetProperty("calendarId").GetGuid().Should().Be(calendarEvent.CalendarId);
    }

    [Fact]
    public void Serialize_IncludesBaseEntityFields()
    {
        var user = CreateUser();

        var json = _sut.Serialize(user);

        json.GetProperty("id").GetGuid().Should().Be(user.Id);
        json.TryGetProperty("createdAt", out _).Should().BeTrue();
        json.TryGetProperty("updatedAt", out _).Should().BeTrue();
    }

    [Fact]
    public void Serialize_EnumsAsStrings()
    {
        var task = new TaskItem
        {
            SpaceId = Guid.NewGuid(),
            Title = "Test Task",
            Status = TaskItemStatus.InProgress,
            Priority = TaskItemPriority.High,
        };

        var json = _sut.Serialize(task);

        json.GetProperty("status").GetString().Should().Be("inProgress");
        json.GetProperty("priority").GetString().Should().Be("high");
    }

    [Fact]
    public void Serialize_OmitsNullProperties()
    {
        var user = CreateUser();

        var json = _sut.Serialize(user);

        // Locale is null, should be omitted
        json.TryGetProperty("locale", out _).Should().BeFalse();
    }

    // ── Roundtrip ─────────────────────────────────────────────────────

    [Fact]
    public void Serialize_ThenDeserialize_RoundTripsScalarProperties()
    {
        var original = CreateCalendarEvent();

        var json = _sut.Serialize(original);
        var deserialized = (CalendarEvent)_sut.Deserialize(
            json, ChangeLogEntityType.CalendarEvent);

        deserialized.Id.Should().Be(original.Id);
        deserialized.CalendarId.Should().Be(original.CalendarId);
        deserialized.Title.Should().Be(original.Title);
        deserialized.IsAllDay.Should().Be(original.IsAllDay);
        deserialized.StartAt.Should().Be(original.StartAt);
        deserialized.EndAt.Should().Be(original.EndAt);
        deserialized.Timezone.Should().Be(original.Timezone);
        deserialized.Location.Should().Be(original.Location);
    }

    [Fact]
    public void Deserialize_ReturnsCorrectClrType()
    {
        var user = CreateUser();
        var json = _sut.Serialize(user);

        var result = _sut.Deserialize(json, ChangeLogEntityType.User);

        result.Should().BeOfType<User>();
        ((User)result).DisplayName.Should().Be(user.DisplayName);
    }

    [Fact]
    public void Deserialize_ThrowsForInvalidJson()
    {
        using var doc = System.Text.Json.JsonDocument.Parse("{}");
        var emptyJson = doc.RootElement.Clone();

        // Missing required properties should throw or return with defaults.
        // STJ doesn't throw for missing non-required properties, but 'required' keyword causes throw.
        var act = () => _sut.Deserialize(emptyJson, ChangeLogEntityType.CalendarEvent);

        act.Should().Throw<System.Text.Json.JsonException>();
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        DisplayName = "Jane Doe",
        Email = "jane@example.com",
        Timezone = "America/Chicago",
        WeekStart = WeekStart.Monday,
    };

    private static CalendarEvent CreateCalendarEvent() => new()
    {
        Id = Guid.NewGuid(),
        CalendarId = Guid.NewGuid(),
        Title = "Team Standup",
        IsAllDay = false,
        StartAt = new DateTime(2025, 6, 15, 9, 0, 0, DateTimeKind.Utc),
        EndAt = new DateTime(2025, 6, 15, 9, 30, 0, DateTimeKind.Utc),
        Timezone = "America/Chicago",
        Location = "Conference Room A",
    };
}
