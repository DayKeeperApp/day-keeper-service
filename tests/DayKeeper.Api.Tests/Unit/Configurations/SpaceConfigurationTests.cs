using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Persistence.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Configurations;

public sealed class SpaceConfigurationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SpaceTestDbContext _context;

    public SpaceConfigurationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SpaceTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new SpaceTestDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task TenantIdAndNormalizedName_UniqueIndex_RejectsDuplicate()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        _context.Spaces.Add(CreateSpace(tenant.Id, "Family", "family"));
        await _context.SaveChangesAsync();

        _context.Spaces.Add(CreateSpace(tenant.Id, "Family", "family"));

        var act = () => _context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task TenantIdAndNormalizedName_UniqueIndex_AllowsSameNameDifferentTenant()
    {
        var tenantA = new Tenant { Name = "Acme", Slug = "acme" };
        var tenantB = new Tenant { Name = "Beta", Slug = "beta" };
        _context.Tenants.AddRange(tenantA, tenantB);
        await _context.SaveChangesAsync();

        _context.Spaces.Add(CreateSpace(tenantA.Id, "Family", "family"));
        _context.Spaces.Add(CreateSpace(tenantB.Id, "Family", "family"));

        var act = () => _context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Model_HasUniqueCompositeIndexOnTenantIdAndNormalizedName()
    {
        var entityType = _context.Model.FindEntityType(typeof(Space))!;
        var compositeIndex = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 2
                && string.Equals(i.Properties[0].Name, nameof(Space.TenantId), StringComparison.Ordinal)
                && string.Equals(i.Properties[1].Name, nameof(Space.NormalizedName), StringComparison.Ordinal));

        compositeIndex.Should().NotBeNull();
        compositeIndex!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task Tenant_CascadeDelete_RemovesSpaces()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        _context.Spaces.Add(CreateSpace(tenant.Id, "Family", "family"));
        _context.Spaces.Add(CreateSpace(tenant.Id, "Work", "work"));
        await _context.SaveChangesAsync();

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var remainingSpaces = await _context.Spaces.ToListAsync();
        remainingSpaces.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static Space CreateSpace(Guid tenantId, string name, string normalizedName) => new()
    {
        TenantId = tenantId,
        Name = name,
        NormalizedName = normalizedName,
        SpaceType = SpaceType.Shared,
    };

    private sealed class SpaceTestDbContext(
        DbContextOptions<SpaceTestDbContext> options) : DbContext(options)
    {
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Space> Spaces => Set<Space>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new TenantConfiguration().Configure(modelBuilder.Entity<Tenant>());
            new SpaceConfiguration().Configure(modelBuilder.Entity<Space>());
        }
    }
}
