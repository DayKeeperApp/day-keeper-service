using DayKeeper.Api.Tests.Helpers;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Persistence;

public sealed class TenantQueryFilterTests : IDisposable
{
    private static readonly Guid _tenantAId = Guid.NewGuid();
    private static readonly Guid _tenantBId = Guid.NewGuid();

    private readonly SqliteConnection _connection;
    private readonly TestTenantContext _tenantContext;
    private readonly DayKeeperDbContext _context;

    public TenantQueryFilterTests()
    {
        _tenantContext = new TestTenantContext { CurrentTenantId = _tenantAId };

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DayKeeperDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new DayKeeperDbContext(options, _tenantContext);
        _context.Database.EnsureCreated();

        SeedData();
    }

    private void SeedData()
    {
        // Tenants
        var tenantA = new Tenant { Id = _tenantAId, Name = "Tenant A", Slug = "tenant-a" };
        var tenantB = new Tenant { Id = _tenantBId, Name = "Tenant B", Slug = "tenant-b" };
        _context.Set<Tenant>().AddRange(tenantA, tenantB);

        // Users (ITenantScoped)
        _context.Set<User>().AddRange(
            new User
            {
                TenantId = _tenantAId,
                DisplayName = "Alice",
                Email = "alice@a.com",
                Timezone = "UTC",
                WeekStart = WeekStart.Monday
            },
            new User
            {
                TenantId = _tenantBId,
                DisplayName = "Bob",
                Email = "bob@b.com",
                Timezone = "UTC",
                WeekStart = WeekStart.Monday
            });

        // Categories (IOptionalTenantScoped)
        _context.Set<Category>().AddRange(
            new Category
            {
                TenantId = _tenantAId,
                Name = "Work",
                NormalizedName = "work",
                Color = "#000"
            },
            new Category
            {
                TenantId = _tenantBId,
                Name = "Home",
                NormalizedName = "home",
                Color = "#111"
            },
            new Category
            {
                TenantId = null,
                Name = "System",
                NormalizedName = "system",
                Color = "#FFF"
            });

        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task TenantScoped_ReturnsOnlyCurrentTenantRecords()
    {
        var users = await _context.Set<User>().ToListAsync();

        users.Should().ContainSingle()
            .Which.DisplayName.Should().Be("Alice");
    }

    [Fact]
    public async Task TenantScoped_ExcludesOtherTenantRecords()
    {
        var users = await _context.Set<User>().ToListAsync();

        users.Should().NotContain(u => u.TenantId == _tenantBId);
    }

    [Fact]
    public async Task OptionalTenantScoped_ReturnsCurrentTenantAndSystemRecords()
    {
        var categories = await _context.Set<Category>().ToListAsync();

        categories.Should().HaveCount(2);
        categories.Should().Contain(c => c.Name == "Work");
        categories.Should().Contain(c => c.Name == "System");
    }

    [Fact]
    public async Task OptionalTenantScoped_ExcludesOtherTenantRecords()
    {
        var categories = await _context.Set<Category>().ToListAsync();

        categories.Should().NotContain(c => c.Name == "Home");
    }

    [Fact]
    public async Task NullTenantContext_ReturnsAllRecords()
    {
        _tenantContext.CurrentTenantId = null;

        var users = await _context.Set<User>().ToListAsync();
        var categories = await _context.Set<Category>().ToListAsync();

        users.Should().HaveCount(2);
        categories.Should().HaveCount(3);
    }

    [Fact]
    public async Task SoftDeletedRecords_StillExcluded_WithTenantFilter()
    {
        // Soft-delete Alice
        var alice = await _context.Set<User>().SingleAsync();
        alice.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var users = await _context.Set<User>().ToListAsync();

        users.Should().BeEmpty();
    }

    [Fact]
    public async Task IgnoreQueryFilters_BypassesBothFilters()
    {
        // Soft-delete Alice
        var alice = await _context.Set<User>().SingleAsync();
        alice.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var users = await _context.Set<User>().IgnoreQueryFilters().ToListAsync();

        // Should see both Alice (soft-deleted, tenant A) and Bob (tenant B)
        users.Should().HaveCount(2);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
