using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Persistence.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Configurations;

public sealed class ListItemConfigurationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ListItemTestDbContext _context;

    public ListItemConfigurationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ListItemTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ListItemTestDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public void Model_HasCompositeIndexOnShoppingListIdIsCheckedSortOrder()
    {
        var entityType = _context.Model.FindEntityType(typeof(ListItem))!;
        var compositeIndex = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 3
                && string.Equals(i.Properties[0].Name, nameof(ListItem.ShoppingListId), StringComparison.Ordinal)
                && string.Equals(i.Properties[1].Name, nameof(ListItem.IsChecked), StringComparison.Ordinal)
                && string.Equals(i.Properties[2].Name, nameof(ListItem.SortOrder), StringComparison.Ordinal));

        compositeIndex.Should().NotBeNull();
    }

    [Fact]
    public void Model_QuantityHasPrecision18Scale4()
    {
        var entityType = _context.Model.FindEntityType(typeof(ListItem))!;
        var quantityProperty = entityType.FindProperty(nameof(ListItem.Quantity))!;

        quantityProperty.GetPrecision().Should().Be(18);
        quantityProperty.GetScale().Should().Be(4);
    }

    [Fact]
    public async Task ShoppingList_CascadeDelete_RemovesListItems()
    {
        var (_, shoppingList) = await SeedShoppingListAsync();

        _context.ListItems.Add(CreateListItem(shoppingList.Id, "Milk", 0));
        _context.ListItems.Add(CreateListItem(shoppingList.Id, "Bread", 1));
        await _context.SaveChangesAsync();

        _context.ShoppingLists.Remove(shoppingList);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var remaining = await _context.ListItems.ToListAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task Space_CascadeDelete_RemovesListItemsViaShoppingList()
    {
        var (space, shoppingList) = await SeedShoppingListAsync();

        _context.ListItems.Add(CreateListItem(shoppingList.Id, "Milk", 0));
        await _context.SaveChangesAsync();

        _context.Spaces.Remove(space);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var remainingLists = await _context.ShoppingLists.ToListAsync();
        var remainingItems = await _context.ListItems.ToListAsync();
        remainingLists.Should().BeEmpty();
        remainingItems.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task<(Space Space, ShoppingList ShoppingList)> SeedShoppingListAsync()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        var space = new Space
        {
            TenantId = tenant.Id,
            Name = "Test Space",
            NormalizedName = "test space",
            SpaceType = SpaceType.Personal,
        };
        _context.Spaces.Add(space);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        var shoppingList = new ShoppingList
        {
            SpaceId = space.Id,
            Name = "Groceries",
            NormalizedName = "groceries",
        };
        _context.ShoppingLists.Add(shoppingList);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        return (space, shoppingList);
    }

    private static ListItem CreateListItem(Guid shoppingListId, string name, int sortOrder) => new()
    {
        ShoppingListId = shoppingListId,
        Name = name,
        Quantity = 1m,
        IsChecked = false,
        SortOrder = sortOrder,
    };

    private sealed class ListItemTestDbContext(
        DbContextOptions<ListItemTestDbContext> options) : DbContext(options)
    {
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Space> Spaces => Set<Space>();
        public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
        public DbSet<ListItem> ListItems => Set<ListItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new TenantConfiguration().Configure(modelBuilder.Entity<Tenant>());
            new SpaceConfiguration().Configure(modelBuilder.Entity<Space>());
            new ShoppingListConfiguration().Configure(modelBuilder.Entity<ShoppingList>());
            new ListItemConfiguration().Configure(modelBuilder.Entity<ListItem>());
        }
    }
}
