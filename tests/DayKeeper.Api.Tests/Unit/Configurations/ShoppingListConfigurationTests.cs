using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Persistence.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Configurations;

public sealed class ShoppingListConfigurationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ShoppingListTestDbContext _context;

    public ShoppingListConfigurationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ShoppingListTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ShoppingListTestDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task SpaceIdAndNormalizedName_UniqueIndex_RejectsDuplicate()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var space = CreateSpace(tenant.Id);
        _context.Spaces.Add(space);
        await _context.SaveChangesAsync();

        _context.ShoppingLists.Add(CreateShoppingList(space.Id, "Groceries", "groceries"));
        await _context.SaveChangesAsync();

        _context.ShoppingLists.Add(CreateShoppingList(space.Id, "Groceries", "groceries"));

        var act = () => _context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SpaceIdAndNormalizedName_UniqueIndex_AllowsSameNameDifferentSpace()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var spaceA = CreateSpace(tenant.Id);
        var spaceB = CreateSpace(tenant.Id);
        _context.Spaces.AddRange(spaceA, spaceB);
        await _context.SaveChangesAsync();

        _context.ShoppingLists.Add(CreateShoppingList(spaceA.Id, "Groceries", "groceries"));
        _context.ShoppingLists.Add(CreateShoppingList(spaceB.Id, "Groceries", "groceries"));

        var act = () => _context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Model_HasUniqueCompositeIndexOnSpaceIdAndNormalizedName()
    {
        var entityType = _context.Model.FindEntityType(typeof(ShoppingList))!;
        var compositeIndex = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 2
                && string.Equals(i.Properties[0].Name, nameof(ShoppingList.SpaceId), StringComparison.Ordinal)
                && string.Equals(i.Properties[1].Name, nameof(ShoppingList.NormalizedName), StringComparison.Ordinal));

        compositeIndex.Should().NotBeNull();
        compositeIndex!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public void Model_HasCompositeIndexOnSpaceIdAndUpdatedAt()
    {
        var entityType = _context.Model.FindEntityType(typeof(ShoppingList))!;
        var compositeIndex = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 2
                && string.Equals(i.Properties[0].Name, nameof(ShoppingList.SpaceId), StringComparison.Ordinal)
                && string.Equals(i.Properties[1].Name, nameof(ShoppingList.UpdatedAt), StringComparison.Ordinal));

        compositeIndex.Should().NotBeNull();
    }

    [Fact]
    public async Task Space_CascadeDelete_RemovesShoppingLists()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var space = CreateSpace(tenant.Id);
        _context.Spaces.Add(space);
        await _context.SaveChangesAsync();

        _context.ShoppingLists.Add(CreateShoppingList(space.Id, "Groceries", "groceries"));
        _context.ShoppingLists.Add(CreateShoppingList(space.Id, "Hardware", "hardware"));
        await _context.SaveChangesAsync();

        _context.Spaces.Remove(space);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var remaining = await _context.ShoppingLists.ToListAsync();
        remaining.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static Space CreateSpace(Guid tenantId) => new()
    {
        TenantId = tenantId,
        Name = $"Space-{Guid.NewGuid():N}",
        NormalizedName = $"space-{Guid.NewGuid():N}",
        SpaceType = SpaceType.Personal,
    };

    private static ShoppingList CreateShoppingList(Guid spaceId, string name, string normalizedName) => new()
    {
        SpaceId = spaceId,
        Name = name,
        NormalizedName = normalizedName,
    };

    private sealed class ShoppingListTestDbContext(
        DbContextOptions<ShoppingListTestDbContext> options) : DbContext(options)
    {
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Space> Spaces => Set<Space>();
        public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new TenantConfiguration().Configure(modelBuilder.Entity<Tenant>());
            new SpaceConfiguration().Configure(modelBuilder.Entity<Space>());
            new ShoppingListConfiguration().Configure(modelBuilder.Entity<ShoppingList>());
        }
    }
}
