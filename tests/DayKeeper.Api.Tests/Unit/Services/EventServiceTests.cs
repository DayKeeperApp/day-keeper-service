using DayKeeper.Api.Tests.Helpers;
using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Persistence;
using DayKeeper.Infrastructure.Persistence.Repositories;
using DayKeeper.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Services;

public sealed class EventServiceTests : IDisposable
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _spaceId = Guid.NewGuid();
    private static readonly Guid _calendarId = Guid.NewGuid();
    private static readonly Guid _calendar2Id = Guid.NewGuid();
    private static readonly Guid _eventTypeId = Guid.NewGuid();
    private static readonly DateTime _fixedTime =
        new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DayKeeperDbContext _context;
    private readonly IRecurrenceExpander _recurrenceExpander;
    private readonly EventService _sut;

    public EventServiceTests()
    {
        var tenantContext = new TestTenantContext { CurrentTenantId = _tenantId };
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(_fixedTime);

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DayKeeperDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new DayKeeperDbContext(options, tenantContext);
        _context.Database.EnsureCreated();

        var eventRepository = new Repository<CalendarEvent>(_context, dateTimeProvider);
        var calendarRepository = new Repository<Calendar>(_context, dateTimeProvider);
        var eventTypeRepository = new Repository<EventType>(_context, dateTimeProvider);
        _recurrenceExpander = Substitute.For<IRecurrenceExpander>();

        SeedData();

        _sut = new EventService(
            eventRepository, calendarRepository, eventTypeRepository,
            _recurrenceExpander, _context);
    }

    private void SeedData()
    {
        _context.Set<Tenant>().Add(new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
        });
        _context.Set<Space>().Add(new Space
        {
            Id = _spaceId,
            TenantId = _tenantId,
            Name = "Test Space",
            NormalizedName = "test space",
            SpaceType = SpaceType.Personal,
        });
        _context.Set<Calendar>().Add(new Calendar
        {
            Id = _calendarId,
            SpaceId = _spaceId,
            Name = "Calendar One",
            NormalizedName = "calendar one",
            Color = "#4A90D9",
            IsDefault = true,
        });
        _context.Set<Calendar>().Add(new Calendar
        {
            Id = _calendar2Id,
            SpaceId = _spaceId,
            Name = "Calendar Two",
            NormalizedName = "calendar two",
            Color = "#FF5733",
            IsDefault = false,
        });
        _context.Set<EventType>().Add(new EventType
        {
            Id = _eventTypeId,
            TenantId = _tenantId,
            Name = "Meeting",
            NormalizedName = "meeting",
            Color = "#00FF00",
        });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    private static DateTime Utc(int y, int m, int d, int h = 0, int min = 0) =>
        new(y, m, d, h, min, 0, DateTimeKind.Utc);

    // ── CreateEventAsync ─────────────────────────────────────────────

    [Fact]
    public async Task CreateEventAsync_WhenValid_ReturnsEvent()
    {
        var result = await _sut.CreateEventAsync(
            _calendarId, "Standup", "Daily sync", false,
            Utc(2026, 3, 1, 15), Utc(2026, 3, 1, 15, 30),
            null, null, "America/Chicago",
            null, null, null, null);

        result.Should().NotBeNull();
        result.Title.Should().Be("Standup");
        result.CalendarId.Should().Be(_calendarId);
        result.Timezone.Should().Be("America/Chicago");
    }

    [Fact]
    public async Task CreateEventAsync_WhenCalendarNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CreateEventAsync(
            Guid.NewGuid(), "Orphan", null, false,
            Utc(2026, 3, 1, 15), Utc(2026, 3, 1, 16),
            null, null, "UTC", null, null, null, null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CreateEventAsync_WhenEventTypeNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CreateEventAsync(
            _calendarId, "Bad Type", null, false,
            Utc(2026, 3, 1, 15), Utc(2026, 3, 1, 16),
            null, null, "UTC", null, null, null, Guid.NewGuid());

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CreateEventAsync_WithNullEventType_Succeeds()
    {
        var result = await _sut.CreateEventAsync(
            _calendarId, "No Type", null, false,
            Utc(2026, 3, 1, 15), Utc(2026, 3, 1, 16),
            null, null, "UTC", null, null, null, null);

        result.EventTypeId.Should().BeNull();
    }

    [Fact]
    public async Task CreateEventAsync_WithRecurrenceRule_StoresRule()
    {
        var result = await _sut.CreateEventAsync(
            _calendarId, "Recurring", null, false,
            Utc(2026, 3, 1, 15), Utc(2026, 3, 1, 16),
            null, null, "America/Chicago",
            "FREQ=DAILY;COUNT=10", Utc(2026, 3, 10, 15),
            null, null);

        result.RecurrenceRule.Should().Be("FREQ=DAILY;COUNT=10");
        result.RecurrenceEndAt.Should().Be(Utc(2026, 3, 10, 15));
    }

    // ── UpdateEventAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateEventAsync_WhenValid_UpdatesFields()
    {
        var created = await _sut.CreateEventAsync(
            _calendarId, "Original", "Desc", false,
            Utc(2026, 3, 1, 15), Utc(2026, 3, 1, 16),
            null, null, "UTC", null, null, "Room A", null);

        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateEventAsync(
            created.Id, "Updated", null, null,
            null, null, null, null, null, null, null, "Room B", null);

        result.Title.Should().Be("Updated");
        result.Location.Should().Be("Room B");
    }

    [Fact]
    public async Task UpdateEventAsync_WhenEventNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.UpdateEventAsync(
            Guid.NewGuid(), "Title", null, null,
            null, null, null, null, null, null, null, null, null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UpdateEventAsync_WhenEventTypeSetToEmpty_ClearsEventType()
    {
        var created = await _sut.CreateEventAsync(
            _calendarId, "With Type", null, false,
            Utc(2026, 3, 1, 15), Utc(2026, 3, 1, 16),
            null, null, "UTC", null, null, null, _eventTypeId);

        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateEventAsync(
            created.Id, null, null, null,
            null, null, null, null, null, null, null, null, Guid.Empty);

        result.EventTypeId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateEventAsync_WhenNullFields_LeavesUnchanged()
    {
        var created = await _sut.CreateEventAsync(
            _calendarId, "Original", "Description", false,
            Utc(2026, 3, 1, 15), Utc(2026, 3, 1, 16),
            null, null, "America/Chicago", null, null, "Room A", null);

        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateEventAsync(
            created.Id, null, null, null,
            null, null, null, null, null, null, null, null, null);

        result.Title.Should().Be("Original");
        result.Description.Should().Be("Description");
        result.Location.Should().Be("Room A");
        result.Timezone.Should().Be("America/Chicago");
    }

    // ── DeleteEventAsync ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteEventAsync_WhenEventExists_ReturnsTrue()
    {
        var created = await _sut.CreateEventAsync(
            _calendarId, "To Delete", null, false,
            Utc(2026, 3, 1, 15), Utc(2026, 3, 1, 16),
            null, null, "UTC", null, null, null, null);

        var result = await _sut.DeleteEventAsync(created.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteEventAsync_WhenEventDoesNotExist_ReturnsFalse()
    {
        var result = await _sut.DeleteEventAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── GetEventsForRangeAsync (4-phase expansion) ───────────────────

    [Fact]
    public async Task GetEventsForRange_SingleEvent_InRange_ReturnsOccurrence()
    {
        await _sut.CreateEventAsync(
            _calendarId, "In Range", null, false,
            Utc(2026, 3, 5, 10), Utc(2026, 3, 5, 11),
            null, null, "UTC", null, null, null, null);

        _context.ChangeTracker.Clear();

        var result = await _sut.GetEventsForRangeAsync(
            [_calendarId], Utc(2026, 3, 1), Utc(2026, 3, 10), "UTC");

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("In Range");
        result[0].IsRecurring.Should().BeFalse();
        result[0].IsException.Should().BeFalse();
    }

    [Fact]
    public async Task GetEventsForRange_SingleEvent_OutOfRange_ReturnsEmpty()
    {
        await _sut.CreateEventAsync(
            _calendarId, "Out of Range", null, false,
            Utc(2026, 4, 1, 10), Utc(2026, 4, 1, 11),
            null, null, "UTC", null, null, null, null);

        _context.ChangeTracker.Clear();

        var result = await _sut.GetEventsForRangeAsync(
            [_calendarId], Utc(2026, 3, 1), Utc(2026, 3, 31), "UTC");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEventsForRange_RecurringEvent_ExpandsOccurrences()
    {
        var master = await _sut.CreateEventAsync(
            _calendarId, "Daily Standup", null, false,
            Utc(2026, 3, 1, 15), Utc(2026, 3, 1, 15, 30),
            null, null, "America/Chicago",
            "FREQ=DAILY;COUNT=5", Utc(2026, 3, 5, 15),
            null, null);

        // Configure the recurrence expander mock to return 3 occurrences in range
        var expandedTimestamps = new List<DateTime>
        {
            Utc(2026, 3, 1, 15),
            Utc(2026, 3, 2, 15),
            Utc(2026, 3, 3, 15),
        };
        _recurrenceExpander.GetOccurrences(
            "FREQ=DAILY;COUNT=5",
            Utc(2026, 3, 1, 15),
            "UTC",
            Utc(2026, 3, 1),
            Utc(2026, 3, 4))
            .Returns(expandedTimestamps);

        _context.ChangeTracker.Clear();

        var result = await _sut.GetEventsForRangeAsync(
            [_calendarId], Utc(2026, 3, 1), Utc(2026, 3, 4), "UTC");

        result.Should().Contain(o => o.IsRecurring && o.Title == "Daily Standup");
        var recurring = result.Where(o => o.IsRecurring).ToList();
        recurring.Should().HaveCount(3);
        recurring.Should().AllSatisfy(o =>
        {
            o.CalendarEventId.Should().Be(master.Id);
            o.IsException.Should().BeFalse();
        });
    }

    [Fact]
    public async Task GetEventsForRange_RecurrenceException_ModifiesOccurrence()
    {
        var master = await _sut.CreateEventAsync(
            _calendarId, "Daily Standup", null, false,
            Utc(2026, 3, 1, 15), Utc(2026, 3, 1, 15, 30),
            null, null, "America/Chicago",
            "FREQ=DAILY", null,
            null, null);

        // Add a recurrence exception that modifies the March 2 occurrence
        _context.Set<RecurrenceException>().Add(new RecurrenceException
        {
            CalendarEventId = master.Id,
            OriginalStartAt = Utc(2026, 3, 2, 15),
            IsCancelled = false,
            Title = "Modified Standup",
            Location = "Room B",
        });
        await _context.SaveChangesAsync();

        _recurrenceExpander.GetOccurrences(
            "FREQ=DAILY",
            Utc(2026, 3, 1, 15),
            "UTC",
            Utc(2026, 3, 1),
            Utc(2026, 3, 4))
            .Returns(new List<DateTime>
            {
                Utc(2026, 3, 1, 15),
                Utc(2026, 3, 2, 15),
                Utc(2026, 3, 3, 15),
            });

        _context.ChangeTracker.Clear();

        var result = await _sut.GetEventsForRangeAsync(
            [_calendarId], Utc(2026, 3, 1), Utc(2026, 3, 4), "UTC");

        var modified = result.FirstOrDefault(o => o.IsException);
        modified.Should().NotBeNull();
        modified!.Title.Should().Be("Modified Standup");
        modified.Location.Should().Be("Room B");
        modified.OriginalStartAt.Should().Be(Utc(2026, 3, 2, 15));
    }

    [Fact]
    public async Task GetEventsForRange_CancelledRecurrenceException_SkipsOccurrence()
    {
        var master = await _sut.CreateEventAsync(
            _calendarId, "Daily Standup", null, false,
            Utc(2026, 3, 1, 15), Utc(2026, 3, 1, 15, 30),
            null, null, "America/Chicago",
            "FREQ=DAILY", null,
            null, null);

        // Cancel the March 2 occurrence
        _context.Set<RecurrenceException>().Add(new RecurrenceException
        {
            CalendarEventId = master.Id,
            OriginalStartAt = Utc(2026, 3, 2, 15),
            IsCancelled = true,
        });
        await _context.SaveChangesAsync();

        _recurrenceExpander.GetOccurrences(
            "FREQ=DAILY",
            Utc(2026, 3, 1, 15),
            "UTC",
            Utc(2026, 3, 1),
            Utc(2026, 3, 4))
            .Returns(new List<DateTime>
            {
                Utc(2026, 3, 1, 15),
                Utc(2026, 3, 2, 15),
                Utc(2026, 3, 3, 15),
            });

        _context.ChangeTracker.Clear();

        var result = await _sut.GetEventsForRangeAsync(
            [_calendarId], Utc(2026, 3, 1), Utc(2026, 3, 4), "UTC");

        var recurring = result.Where(o => o.IsRecurring).ToList();
        recurring.Should().HaveCount(2); // Mar 2 cancelled
        recurring.Should().NotContain(o => o.OriginalStartAt == Utc(2026, 3, 2, 15));
    }

    [Fact]
    public async Task GetEventsForRange_ExpiredRecurringSeries_IsExcluded()
    {
        // Create a recurring event that ended before the query range
        await _sut.CreateEventAsync(
            _calendarId, "Expired Series", null, false,
            Utc(2026, 1, 1, 10), Utc(2026, 1, 1, 11),
            null, null, "UTC",
            "FREQ=DAILY;COUNT=5", Utc(2026, 1, 5, 10),
            null, null);

        _context.ChangeTracker.Clear();

        // Query in March — the expired series should be filtered by RecurrenceEndAt < rangeStart
        var result = await _sut.GetEventsForRangeAsync(
            [_calendarId], Utc(2026, 3, 1), Utc(2026, 3, 31), "UTC");

        result.Should().BeEmpty();
        // The recurrence expander should never be called for expired series
        _recurrenceExpander.DidNotReceive().GetOccurrences(
            Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<string>(),
            Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task GetEventsForRange_MixedSingleAndRecurring_SortsByStartAt()
    {
        // Single event at March 2 10:00
        await _sut.CreateEventAsync(
            _calendarId, "Single Event", null, false,
            Utc(2026, 3, 2, 10), Utc(2026, 3, 2, 11),
            null, null, "UTC", null, null, null, null);

        // Recurring event starting March 1 15:00
        var master = await _sut.CreateEventAsync(
            _calendarId, "Recurring Event", null, false,
            Utc(2026, 3, 1, 15), Utc(2026, 3, 1, 15, 30),
            null, null, "UTC",
            "FREQ=DAILY", null,
            null, null);

        _recurrenceExpander.GetOccurrences(
            "FREQ=DAILY",
            Utc(2026, 3, 1, 15),
            "UTC",
            Utc(2026, 3, 1),
            Utc(2026, 3, 4))
            .Returns(new List<DateTime>
            {
                Utc(2026, 3, 1, 15),
                Utc(2026, 3, 2, 15),
                Utc(2026, 3, 3, 15),
            });

        _context.ChangeTracker.Clear();

        var result = await _sut.GetEventsForRangeAsync(
            [_calendarId], Utc(2026, 3, 1), Utc(2026, 3, 4), "UTC");

        result.Should().HaveCount(4); // 1 single + 3 recurring
        result.Should().BeInAscendingOrder(o => o.StartAt);
    }

    [Fact]
    public async Task GetEventsForRange_NoRecurringEvents_SkipsExpansion()
    {
        await _sut.CreateEventAsync(
            _calendarId, "Single Only", null, false,
            Utc(2026, 3, 5, 10), Utc(2026, 3, 5, 11),
            null, null, "UTC", null, null, null, null);

        _context.ChangeTracker.Clear();

        var result = await _sut.GetEventsForRangeAsync(
            [_calendarId], Utc(2026, 3, 1), Utc(2026, 3, 10), "UTC");

        result.Should().HaveCount(1);
        _recurrenceExpander.DidNotReceive().GetOccurrences(
            Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<string>(),
            Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task GetEventsForRange_MultipleCalendars_ReturnsAllMatching()
    {
        await _sut.CreateEventAsync(
            _calendarId, "Cal1 Event", null, false,
            Utc(2026, 3, 5, 10), Utc(2026, 3, 5, 11),
            null, null, "UTC", null, null, null, null);

        await _sut.CreateEventAsync(
            _calendar2Id, "Cal2 Event", null, false,
            Utc(2026, 3, 5, 14), Utc(2026, 3, 5, 15),
            null, null, "UTC", null, null, null, null);

        _context.ChangeTracker.Clear();

        var result = await _sut.GetEventsForRangeAsync(
            [_calendarId, _calendar2Id], Utc(2026, 3, 1), Utc(2026, 3, 10), "UTC");

        result.Should().HaveCount(2);
        result.Should().Contain(o => o.Title == "Cal1 Event");
        result.Should().Contain(o => o.Title == "Cal2 Event");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
