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

public sealed class UserServiceTests : IDisposable
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _otherTenantId = Guid.NewGuid();
    private static readonly Guid _existingUserId = Guid.NewGuid();
    private static readonly DateTime _fixedTime =
        new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DayKeeperDbContext _context;
    private readonly UserService _sut;

    public UserServiceTests()
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

        var userRepository = new Repository<User>(_context, dateTimeProvider);
        var tenantRepository = new Repository<Tenant>(_context, dateTimeProvider);

        SeedData();

        _sut = new UserService(userRepository, tenantRepository, _context);
    }

    private void SeedData()
    {
        _context.Set<Tenant>().Add(new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
        });
        _context.Set<Tenant>().Add(new Tenant
        {
            Id = _otherTenantId,
            Name = "Other Tenant",
            Slug = "other-tenant",
        });
        _context.Set<User>().Add(new User
        {
            Id = _existingUserId,
            TenantId = _tenantId,
            DisplayName = "Existing User",
            Email = "existing@test.com",
            Timezone = "America/New_York",
            WeekStart = WeekStart.Monday,
            Locale = "en-US",
        });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    // ── CreateUserAsync ─────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_WhenValid_ReturnsUser()
    {
        var result = await _sut.CreateUserAsync(
            _tenantId, "New User", "new@test.com", "UTC", WeekStart.Sunday);

        result.Should().NotBeNull();
        result.DisplayName.Should().Be("New User");
        result.Email.Should().Be("new@test.com");
        result.TenantId.Should().Be(_tenantId);
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateUserAsync_WhenValid_NormalizesEmail()
    {
        var result = await _sut.CreateUserAsync(
            _tenantId, "Trimmed", "  USER@TEST.COM  ", "UTC", WeekStart.Sunday);

        result.Email.Should().Be("user@test.com");
    }

    [Fact]
    public async Task CreateUserAsync_WhenTenantDoesNotExist_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CreateUserAsync(
            Guid.NewGuid(), "Orphan", "orphan@test.com", "UTC", WeekStart.Sunday);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CreateUserAsync_WhenDuplicateEmail_ThrowsDuplicateEmailException()
    {
        var act = () => _sut.CreateUserAsync(
            _tenantId, "Duplicate", "existing@test.com", "UTC", WeekStart.Sunday);

        await act.Should().ThrowAsync<DuplicateEmailException>();
    }

    // ── GetUserAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetUserAsync_WhenUserExists_ReturnsUser()
    {
        var result = await _sut.GetUserAsync(_existingUserId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(_existingUserId);
        result.DisplayName.Should().Be("Existing User");
    }

    [Fact]
    public async Task GetUserAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetUserAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── GetUserByEmailAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetUserByEmailAsync_WhenEmailExists_ReturnsUser()
    {
        var result = await _sut.GetUserByEmailAsync(_tenantId, "existing@test.com");

        result.Should().NotBeNull();
        result!.Id.Should().Be(_existingUserId);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WhenEmailDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetUserByEmailAsync(_tenantId, "nonexistent@test.com");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByEmailAsync_NormalizesEmailBeforeQuery()
    {
        var result = await _sut.GetUserByEmailAsync(_tenantId, "  EXISTING@TEST.COM  ");

        result.Should().NotBeNull();
        result!.Id.Should().Be(_existingUserId);
    }

    // ── GetUsersByTenantAsync ───────────────────────────────────────

    [Fact]
    public async Task GetUsersByTenantAsync_WhenUsersExist_ReturnsUsers()
    {
        var result = await _sut.GetUsersByTenantAsync(_tenantId);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(_existingUserId);
    }

    [Fact]
    public async Task GetUsersByTenantAsync_WhenNoUsers_ReturnsEmptyList()
    {
        var result = await _sut.GetUsersByTenantAsync(_otherTenantId);

        result.Should().BeEmpty();
    }

    // ── UpdateUserAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserAsync_WhenDisplayNameProvided_UpdatesDisplayName()
    {
        var result = await _sut.UpdateUserAsync(
            _existingUserId, "Updated Name", null, null, null, null);

        result.DisplayName.Should().Be("Updated Name");
        result.Email.Should().Be("existing@test.com");
    }

    [Fact]
    public async Task UpdateUserAsync_WhenEmailProvided_UpdatesEmail()
    {
        var result = await _sut.UpdateUserAsync(
            _existingUserId, null, "updated@test.com", null, null, null);

        result.Email.Should().Be("updated@test.com");
    }

    [Fact]
    public async Task UpdateUserAsync_WhenTimezoneProvided_UpdatesTimezone()
    {
        var result = await _sut.UpdateUserAsync(
            _existingUserId, null, null, "Europe/London", null, null);

        result.Timezone.Should().Be("Europe/London");
    }

    [Fact]
    public async Task UpdateUserAsync_WhenWeekStartProvided_UpdatesWeekStart()
    {
        var result = await _sut.UpdateUserAsync(
            _existingUserId, null, null, null, WeekStart.Saturday, null);

        result.WeekStart.Should().Be(WeekStart.Saturday);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenLocaleProvided_UpdatesLocale()
    {
        var result = await _sut.UpdateUserAsync(
            _existingUserId, null, null, null, null, "fr-FR");

        result.Locale.Should().Be("fr-FR");
    }

    [Fact]
    public async Task UpdateUserAsync_WhenEmptyLocaleProvided_SetsLocaleToNull()
    {
        var result = await _sut.UpdateUserAsync(
            _existingUserId, null, null, null, null, "");

        result.Locale.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserAsync_WhenUserDoesNotExist_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.UpdateUserAsync(
            Guid.NewGuid(), "Name", null, null, null, null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UpdateUserAsync_WhenNewEmailConflicts_ThrowsDuplicateEmailException()
    {
        // Create a second user to conflict with
        await _sut.CreateUserAsync(
            _tenantId, "Second", "second@test.com", "UTC", WeekStart.Sunday);

        var act = () => _sut.UpdateUserAsync(
            _existingUserId, null, "second@test.com", null, null, null);

        await act.Should().ThrowAsync<DuplicateEmailException>();
    }

    [Fact]
    public async Task UpdateUserAsync_WhenSameEmailProvided_DoesNotThrow()
    {
        var act = () => _sut.UpdateUserAsync(
            _existingUserId, null, "existing@test.com", null, null, null);

        await act.Should().NotThrowAsync();
    }

    // ── DeleteUserAsync ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteUserAsync_WhenUserExists_ReturnsTrue()
    {
        var result = await _sut.DeleteUserAsync(_existingUserId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_WhenUserDoesNotExist_ReturnsFalse()
    {
        var result = await _sut.DeleteUserAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
