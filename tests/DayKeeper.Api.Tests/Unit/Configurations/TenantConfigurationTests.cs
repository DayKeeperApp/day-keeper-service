using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Configurations;

public sealed class TenantConfigurationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TenantTestDbContext _context;

    public TenantConfigurationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TenantTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TenantTestDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Slug_UniqueIndex_RejectsDuplicateSlug()
    {
        _context.Tenants.Add(new Tenant { Name = "First", Slug = "shared-slug" });
        await _context.SaveChangesAsync();

        _context.Tenants.Add(new Tenant { Name = "Second", Slug = "shared-slug" });

        var act = () => _context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Slug_UniqueIndex_AllowsDifferentSlugs()
    {
        _context.Tenants.Add(new Tenant { Name = "First", Slug = "slug-one" });
        _context.Tenants.Add(new Tenant { Name = "Second", Slug = "slug-two" });

        var act = () => _context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Model_HasUniqueIndexOnSlug()
    {
        var entityType = _context.Model.FindEntityType(typeof(Tenant))!;
        var slugIndex = entityType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => string.Equals(p.Name, nameof(Tenant.Slug), StringComparison.Ordinal)));

        slugIndex.Should().NotBeNull();
        slugIndex!.IsUnique.Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private sealed class TenantTestDbContext(
        DbContextOptions<TenantTestDbContext> options) : DbContext(options)
    {
        public DbSet<Tenant> Tenants => Set<Tenant>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new TenantConfiguration().Configure(modelBuilder.Entity<Tenant>());
        }
    }
}
