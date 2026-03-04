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

public sealed class CalendarServiceTests : IDisposable
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _spaceId = Guid.NewGuid();
    private static readonly Guid _existingCalendarId = Guid.NewGuid();
    private static readonly DateTime _fixedTime =
        new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DayKeeperDbContext _context;
    private readonly CalendarService _sut;

    public CalendarServiceTests()
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

        var calendarRepository = new Repository<Calendar>(_context, dateTimeProvider);
        var spaceRepository = new Repository<Space>(_context, dateTimeProvider);

        SeedData();

        _sut = new CalendarService(calendarRepository, spaceRepository, _context);
    }

    private void SeedData()
    {
        _context.Set<Tenant>().Add(new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
        });
        _context.Set<User>().Add(new User
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DisplayName = "Owner",
            Email = "owner@test.com",
            Timezone = "UTC",
            WeekStart = WeekStart.Monday,
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
            Id = _existingCalendarId,
            SpaceId = _spaceId,
            Name = "Default Calendar",
            NormalizedName = "default calendar",
            Color = "#4A90D9",
            IsDefault = true,
        });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    // ── CreateCalendarAsync ──────────────────────────────────────────

    [Fact]
    public async Task CreateCalendarAsync_WhenValid_ReturnsCalendar()
    {
        var result = await _sut.CreateCalendarAsync(_spaceId, "Work Calendar", "#FF5733", false);

        result.Should().NotBeNull();
        result.Name.Should().Be("Work Calendar");
        result.Color.Should().Be("#FF5733");
        result.SpaceId.Should().Be(_spaceId);
        result.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCalendarAsync_NormalizesNameToLowercase()
    {
        var result = await _sut.CreateCalendarAsync(_spaceId, "  My CALENDAR  ", "#FF5733", false);

        result.Name.Should().Be("My CALENDAR");
        result.NormalizedName.Should().Be("my calendar");
    }

    [Fact]
    public async Task CreateCalendarAsync_WhenSpaceNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CreateCalendarAsync(Guid.NewGuid(), "Orphan", "#000000", false);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CreateCalendarAsync_WhenDuplicateName_ThrowsDuplicateCalendarNameException()
    {
        var act = () => _sut.CreateCalendarAsync(_spaceId, "Default Calendar", "#000000", false);

        await act.Should().ThrowAsync<DuplicateCalendarNameException>();
    }

    [Fact]
    public async Task CreateCalendarAsync_WhenIsDefault_UnsetsExistingDefault()
    {
        var newDefault = await _sut.CreateCalendarAsync(_spaceId, "New Default", "#00FF00", true);

        newDefault.IsDefault.Should().BeTrue();

        _context.ChangeTracker.Clear();
        var previousDefault = await _context.Set<Calendar>()
            .FirstOrDefaultAsync(c => c.Id == _existingCalendarId);
        previousDefault!.IsDefault.Should().BeFalse();
    }

    // ── GetCalendarAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetCalendarAsync_WhenCalendarExists_ReturnsCalendar()
    {
        var result = await _sut.GetCalendarAsync(_existingCalendarId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(_existingCalendarId);
        result.Name.Should().Be("Default Calendar");
    }

    [Fact]
    public async Task GetCalendarAsync_WhenCalendarDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetCalendarAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── UpdateCalendarAsync ──────────────────────────────────────────

    [Fact]
    public async Task UpdateCalendarAsync_WhenNameProvided_UpdatesNameAndNormalized()
    {
        var result = await _sut.UpdateCalendarAsync(_existingCalendarId, "Updated Calendar", null, null);

        result.Name.Should().Be("Updated Calendar");
        result.NormalizedName.Should().Be("updated calendar");
    }

    [Fact]
    public async Task UpdateCalendarAsync_WhenColorProvided_UpdatesColor()
    {
        var result = await _sut.UpdateCalendarAsync(_existingCalendarId, null, "#FF0000", null);

        result.Color.Should().Be("#FF0000");
        result.Name.Should().Be("Default Calendar"); // unchanged
    }

    [Fact]
    public async Task UpdateCalendarAsync_WhenIsDefaultTrue_UnsetsExistingDefault()
    {
        // Create a second calendar (not default)
        var second = await _sut.CreateCalendarAsync(_spaceId, "Second", "#000000", false);

        // Make second the default
        var result = await _sut.UpdateCalendarAsync(second.Id, null, null, true);

        result.IsDefault.Should().BeTrue();

        _context.ChangeTracker.Clear();
        var previousDefault = await _context.Set<Calendar>()
            .FirstOrDefaultAsync(c => c.Id == _existingCalendarId);
        previousDefault!.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCalendarAsync_WhenCalendarNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.UpdateCalendarAsync(Guid.NewGuid(), "Name", null, null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UpdateCalendarAsync_WhenDuplicateName_ThrowsDuplicateCalendarNameException()
    {
        await _sut.CreateCalendarAsync(_spaceId, "Second Calendar", "#000000", false);

        var act = () => _sut.UpdateCalendarAsync(_existingCalendarId, "Second Calendar", null, null);

        await act.Should().ThrowAsync<DuplicateCalendarNameException>();
    }

    [Fact]
    public async Task UpdateCalendarAsync_WhenSameNameProvided_DoesNotThrow()
    {
        var act = () => _sut.UpdateCalendarAsync(_existingCalendarId, "Default Calendar", null, null);

        await act.Should().NotThrowAsync();
    }

    // ── DeleteCalendarAsync ──────────────────────────────────────────

    [Fact]
    public async Task DeleteCalendarAsync_WhenCalendarExists_ReturnsTrue()
    {
        var result = await _sut.DeleteCalendarAsync(_existingCalendarId);

        result.Should().BeTrue();

        _context.ChangeTracker.Clear();
        var calendar = await _context.Set<Calendar>()
            .FirstOrDefaultAsync(c => c.Id == _existingCalendarId);
        calendar.Should().BeNull(); // soft-deleted, filtered out
    }

    [Fact]
    public async Task DeleteCalendarAsync_WhenCalendarDoesNotExist_ReturnsFalse()
    {
        var result = await _sut.DeleteCalendarAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
