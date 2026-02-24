using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Configurations;

public sealed class UserConfigurationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly UserTestDbContext _context;

    public UserConfigurationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<UserTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new UserTestDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task TenantIdAndEmail_UniqueIndex_RejectsDuplicate()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        _context.Users.Add(CreateUser(tenant.Id, "alice@example.com"));
        await _context.SaveChangesAsync();

        _context.Users.Add(CreateUser(tenant.Id, "alice@example.com"));

        var act = () => _context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task TenantIdAndEmail_UniqueIndex_AllowsSameEmailDifferentTenant()
    {
        var tenantA = new Tenant { Name = "Acme", Slug = "acme" };
        var tenantB = new Tenant { Name = "Beta", Slug = "beta" };
        _context.Tenants.AddRange(tenantA, tenantB);
        await _context.SaveChangesAsync();

        _context.Users.Add(CreateUser(tenantA.Id, "shared@example.com"));
        _context.Users.Add(CreateUser(tenantB.Id, "shared@example.com"));

        var act = () => _context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Model_HasIndexOnTenantId()
    {
        var entityType = _context.Model.FindEntityType(typeof(User))!;
        var tenantIdIndex = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 1
                && string.Equals(i.Properties[0].Name, nameof(User.TenantId), StringComparison.Ordinal));

        tenantIdIndex.Should().NotBeNull();
    }

    [Fact]
    public void Model_HasUniqueCompositeIndexOnTenantIdAndEmail()
    {
        var entityType = _context.Model.FindEntityType(typeof(User))!;
        var compositeIndex = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 2
                && string.Equals(i.Properties[0].Name, nameof(User.TenantId), StringComparison.Ordinal)
                && string.Equals(i.Properties[1].Name, nameof(User.Email), StringComparison.Ordinal));

        compositeIndex.Should().NotBeNull();
        compositeIndex!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task Tenant_CascadeDelete_RemovesUsers()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        _context.Users.Add(CreateUser(tenant.Id, "alice@example.com"));
        _context.Users.Add(CreateUser(tenant.Id, "bob@example.com"));
        await _context.SaveChangesAsync();

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var remainingUsers = await _context.Users.ToListAsync();
        remainingUsers.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static User CreateUser(Guid tenantId, string email) => new()
    {
        TenantId = tenantId,
        DisplayName = "Test User",
        Email = email,
        Timezone = "America/Chicago",
    };

    private sealed class UserTestDbContext(
        DbContextOptions<UserTestDbContext> options) : DbContext(options)
    {
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new TenantConfiguration().Configure(modelBuilder.Entity<Tenant>());
            new UserConfiguration().Configure(modelBuilder.Entity<User>());
        }
    }
}
