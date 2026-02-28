using DayKeeper.Api.Tests.Helpers;
using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence;
using DayKeeper.Infrastructure.Persistence.Repositories;
using DayKeeper.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Services;

public sealed class TenantServiceTests : IDisposable
{
    private static readonly Guid _existingTenantId = Guid.NewGuid();
    private static readonly DateTime _fixedTime =
        new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DayKeeperDbContext _context;
    private readonly TenantService _sut;

    public TenantServiceTests()
    {
        var tenantContext = new TestTenantContext { CurrentTenantId = null };
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(_fixedTime);

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DayKeeperDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new DayKeeperDbContext(options, tenantContext);
        _context.Database.EnsureCreated();

        var tenantRepository = new Repository<Tenant>(_context, dateTimeProvider);

        SeedData();

        _sut = new TenantService(tenantRepository, _context);
    }

    private void SeedData()
    {
        _context.Set<Tenant>().Add(new Tenant
        {
            Id = _existingTenantId,
            Name = "Existing Tenant",
            Slug = "existing-tenant",
        });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    // ── CreateTenantAsync ───────────────────────────────────────────

    [Fact]
    public async Task CreateTenantAsync_WhenSlugIsUnique_ReturnsTenant()
    {
        var result = await _sut.CreateTenantAsync("New Tenant", "new-tenant");

        result.Should().NotBeNull();
        result.Name.Should().Be("New Tenant");
        result.Slug.Should().Be("new-tenant");
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTenantAsync_WhenSlugIsUnique_PersistsTenant()
    {
        var result = await _sut.CreateTenantAsync("Persisted", "persisted-slug");

        _context.ChangeTracker.Clear();
        var found = await _context.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Id == result.Id);
        found.Should().NotBeNull();
        found!.Slug.Should().Be("persisted-slug");
    }

    [Fact]
    public async Task CreateTenantAsync_WhenSlugIsDuplicate_ThrowsDuplicateSlugException()
    {
        var act = () => _sut.CreateTenantAsync("Another", "existing-tenant");

        await act.Should().ThrowAsync<DuplicateSlugException>();
    }

    [Fact]
    public async Task CreateTenantAsync_TrimsAndLowercasesSlug()
    {
        var result = await _sut.CreateTenantAsync("Trimmed", "  MY-SLUG  ");

        result.Slug.Should().Be("my-slug");
    }

    // ── GetTenantAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetTenantAsync_WhenTenantExists_ReturnsTenant()
    {
        var result = await _sut.GetTenantAsync(_existingTenantId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(_existingTenantId);
        result.Name.Should().Be("Existing Tenant");
    }

    [Fact]
    public async Task GetTenantAsync_WhenTenantDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetTenantAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── GetTenantBySlugAsync ────────────────────────────────────────

    [Fact]
    public async Task GetTenantBySlugAsync_WhenSlugExists_ReturnsTenant()
    {
        var result = await _sut.GetTenantBySlugAsync("existing-tenant");

        result.Should().NotBeNull();
        result!.Id.Should().Be(_existingTenantId);
    }

    [Fact]
    public async Task GetTenantBySlugAsync_WhenSlugDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetTenantBySlugAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTenantBySlugAsync_NormalizesSlugBeforeQuery()
    {
        var result = await _sut.GetTenantBySlugAsync("  EXISTING-TENANT  ");

        result.Should().NotBeNull();
        result!.Id.Should().Be(_existingTenantId);
    }

    // ── UpdateTenantAsync ───────────────────────────────────────────

    [Fact]
    public async Task UpdateTenantAsync_WhenNameProvided_UpdatesName()
    {
        var result = await _sut.UpdateTenantAsync(_existingTenantId, "Updated Name", null);

        result.Name.Should().Be("Updated Name");
        result.Slug.Should().Be("existing-tenant");
    }

    [Fact]
    public async Task UpdateTenantAsync_WhenSlugProvided_UpdatesSlug()
    {
        var result = await _sut.UpdateTenantAsync(_existingTenantId, null, "updated-slug");

        result.Slug.Should().Be("updated-slug");
        result.Name.Should().Be("Existing Tenant");
    }

    [Fact]
    public async Task UpdateTenantAsync_WhenBothProvided_UpdatesNameAndSlug()
    {
        var result = await _sut.UpdateTenantAsync(_existingTenantId, "Both Updated", "both-updated");

        result.Name.Should().Be("Both Updated");
        result.Slug.Should().Be("both-updated");
    }

    [Fact]
    public async Task UpdateTenantAsync_WhenTenantDoesNotExist_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.UpdateTenantAsync(Guid.NewGuid(), "Name", null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UpdateTenantAsync_WhenNewSlugConflicts_ThrowsDuplicateSlugException()
    {
        // Create a second tenant to conflict with
        await _sut.CreateTenantAsync("Second", "second-tenant");

        var act = () => _sut.UpdateTenantAsync(_existingTenantId, null, "second-tenant");

        await act.Should().ThrowAsync<DuplicateSlugException>();
    }

    [Fact]
    public async Task UpdateTenantAsync_WhenSameSlugProvided_DoesNotThrow()
    {
        var act = () => _sut.UpdateTenantAsync(_existingTenantId, null, "existing-tenant");

        await act.Should().NotThrowAsync();
    }

    // ── DeleteTenantAsync ───────────────────────────────────────────

    [Fact]
    public async Task DeleteTenantAsync_WhenTenantExists_ReturnsTrue()
    {
        var result = await _sut.DeleteTenantAsync(_existingTenantId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTenantAsync_WhenTenantDoesNotExist_ReturnsFalse()
    {
        var result = await _sut.DeleteTenantAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
