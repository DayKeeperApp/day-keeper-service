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

public sealed class ShoppingListServiceTests : IDisposable
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _spaceId = Guid.NewGuid();
    private static readonly Guid _secondSpaceId = Guid.NewGuid();
    private static readonly DateTime _fixedTime =
        new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DayKeeperDbContext _context;
    private readonly ShoppingListService _sut;

    public ShoppingListServiceTests()
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

        var shoppingListRepository = new Repository<ShoppingList>(_context, dateTimeProvider);
        var listItemRepository = new Repository<ListItem>(_context, dateTimeProvider);
        var spaceRepository = new Repository<Space>(_context, dateTimeProvider);

        SeedData();

        _sut = new ShoppingListService(
            shoppingListRepository, listItemRepository, spaceRepository, _context);
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
        _context.Set<Space>().Add(new Space
        {
            Id = _secondSpaceId,
            TenantId = _tenantId,
            Name = "Second Space",
            NormalizedName = "second space",
            SpaceType = SpaceType.Shared,
        });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    // ── CreateShoppingListAsync ─────────────────────────────────────

    [Fact]
    public async Task CreateShoppingListAsync_WhenValid_ReturnsShoppingList()
    {
        var result = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");

        result.Should().NotBeNull();
        result.Name.Should().Be("Groceries");
        result.NormalizedName.Should().Be("groceries");
        result.SpaceId.Should().Be(_spaceId);
    }

    [Fact]
    public async Task CreateShoppingListAsync_WhenValid_TrimsName()
    {
        var result = await _sut.CreateShoppingListAsync(_spaceId, "  Groceries  ");

        result.Name.Should().Be("Groceries");
        result.NormalizedName.Should().Be("groceries");
    }

    [Fact]
    public async Task CreateShoppingListAsync_NormalizesNameToLowercase()
    {
        var result = await _sut.CreateShoppingListAsync(_spaceId, "MY LIST");

        result.NormalizedName.Should().Be("my list");
    }

    [Fact]
    public async Task CreateShoppingListAsync_WhenSpaceNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CreateShoppingListAsync(Guid.NewGuid(), "Groceries");

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CreateShoppingListAsync_WhenDuplicateName_ThrowsDuplicateShoppingListNameException()
    {
        await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var act = () => _sut.CreateShoppingListAsync(_spaceId, "Groceries");

        await act.Should().ThrowAsync<DuplicateShoppingListNameException>();
    }

    [Fact]
    public async Task CreateShoppingListAsync_WhenDuplicateNameDifferentCase_ThrowsDuplicateShoppingListNameException()
    {
        await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var act = () => _sut.CreateShoppingListAsync(_spaceId, "groceries");

        await act.Should().ThrowAsync<DuplicateShoppingListNameException>();
    }

    [Fact]
    public async Task CreateShoppingListAsync_WhenSameNameDifferentSpace_Succeeds()
    {
        await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var result = await _sut.CreateShoppingListAsync(_secondSpaceId, "Groceries");

        result.Should().NotBeNull();
        result.SpaceId.Should().Be(_secondSpaceId);
    }

    [Fact]
    public async Task CreateShoppingListAsync_SetsClientGeneratedId()
    {
        var result = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");

        result.Id.Should().NotBe(Guid.Empty);
    }

    // ── GetShoppingListAsync ────────────────────────────────────────

    [Fact]
    public async Task GetShoppingListAsync_WhenExists_ReturnsShoppingList()
    {
        var created = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var result = await _sut.GetShoppingListAsync(created.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Groceries");
    }

    [Fact]
    public async Task GetShoppingListAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _sut.GetShoppingListAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── UpdateShoppingListAsync ─────────────────────────────────────

    [Fact]
    public async Task UpdateShoppingListAsync_WhenNameProvided_UpdatesNameAndNormalizedName()
    {
        var created = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateShoppingListAsync(created.Id, "Hardware Store");

        result.Name.Should().Be("Hardware Store");
        result.NormalizedName.Should().Be("hardware store");
    }

    [Fact]
    public async Task UpdateShoppingListAsync_WhenNameIsNull_DoesNotChangeName()
    {
        var created = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateShoppingListAsync(created.Id, null);

        result.Name.Should().Be("Groceries");
        result.NormalizedName.Should().Be("groceries");
    }

    [Fact]
    public async Task UpdateShoppingListAsync_WhenNameTrimmed_TrimsWhitespace()
    {
        var created = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateShoppingListAsync(created.Id, "  Updated  ");

        result.Name.Should().Be("Updated");
        result.NormalizedName.Should().Be("updated");
    }

    [Fact]
    public async Task UpdateShoppingListAsync_WhenNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.UpdateShoppingListAsync(Guid.NewGuid(), "Nope");

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UpdateShoppingListAsync_WhenDuplicateName_ThrowsDuplicateShoppingListNameException()
    {
        await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var second = await _sut.CreateShoppingListAsync(_spaceId, "Hardware");
        _context.ChangeTracker.Clear();

        var act = () => _sut.UpdateShoppingListAsync(second.Id, "Groceries");

        await act.Should().ThrowAsync<DuplicateShoppingListNameException>();
    }

    [Fact]
    public async Task UpdateShoppingListAsync_WhenSameNameProvided_DoesNotThrow()
    {
        var created = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var act = () => _sut.UpdateShoppingListAsync(created.Id, "Groceries");

        await act.Should().NotThrowAsync();
    }

    // ── DeleteShoppingListAsync ─────────────────────────────────────

    [Fact]
    public async Task DeleteShoppingListAsync_WhenExists_ReturnsTrue()
    {
        var created = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var result = await _sut.DeleteShoppingListAsync(created.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteShoppingListAsync_WhenExists_SoftDeletesEntity()
    {
        var created = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        await _sut.DeleteShoppingListAsync(created.Id);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetShoppingListAsync(created.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteShoppingListAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _sut.DeleteShoppingListAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── CreateListItemAsync ─────────────────────────────────────────

    [Fact]
    public async Task CreateListItemAsync_WhenValid_ReturnsListItem()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var result = await _sut.CreateListItemAsync(list.Id, "Milk", 2m, "gallon", 0);

        result.Should().NotBeNull();
        result.Name.Should().Be("Milk");
        result.ShoppingListId.Should().Be(list.Id);
        result.Quantity.Should().Be(2m);
        result.Unit.Should().Be("gallon");
        result.SortOrder.Should().Be(0);
    }

    [Fact]
    public async Task CreateListItemAsync_WhenValid_SetsIsCheckedToFalse()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var result = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);

        result.IsChecked.Should().BeFalse();
    }

    [Fact]
    public async Task CreateListItemAsync_WhenValid_TrimsName()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var result = await _sut.CreateListItemAsync(list.Id, "  Milk  ", 1m, null, 0);

        result.Name.Should().Be("Milk");
    }

    [Fact]
    public async Task CreateListItemAsync_WhenValid_TrimsUnit()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var result = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, "  oz  ", 0);

        result.Unit.Should().Be("oz");
    }

    [Fact]
    public async Task CreateListItemAsync_WhenUnitIsNull_SetsUnitToNull()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var result = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);

        result.Unit.Should().BeNull();
    }

    [Fact]
    public async Task CreateListItemAsync_WhenShoppingListNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CreateListItemAsync(Guid.NewGuid(), "Milk", 1m, null, 0);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CreateListItemAsync_WithZeroQuantity_Succeeds()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var result = await _sut.CreateListItemAsync(list.Id, "Placeholder", 0m, null, 0);

        result.Quantity.Should().Be(0m);
    }

    [Fact]
    public async Task CreateListItemAsync_WithDecimalQuantity_PreservesPrecision()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var result = await _sut.CreateListItemAsync(list.Id, "Milk", 1.5m, "gallon", 0);
        _context.ChangeTracker.Clear();

        var fetched = await _context.Set<ListItem>().FirstAsync(i => i.Id == result.Id);
        fetched.Quantity.Should().Be(1.5m);
    }

    [Fact]
    public async Task CreateListItemAsync_WithHighPrecisionDecimal_PreservesPrecision()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var result = await _sut.CreateListItemAsync(list.Id, "Spice", 0.1234m, "oz", 0);
        _context.ChangeTracker.Clear();

        var fetched = await _context.Set<ListItem>().FirstAsync(i => i.Id == result.Id);
        fetched.Quantity.Should().Be(0.1234m);
    }

    [Fact]
    public async Task CreateListItemAsync_WithLargeQuantity_Succeeds()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var result = await _sut.CreateListItemAsync(list.Id, "Rice", 999999.9999m, "g", 0);
        _context.ChangeTracker.Clear();

        var fetched = await _context.Set<ListItem>().FirstAsync(i => i.Id == result.Id);
        fetched.Quantity.Should().Be(999999.9999m);
    }

    // ── UpdateListItemAsync ─────────────────────────────────────────

    [Fact]
    public async Task UpdateListItemAsync_WhenNameProvided_UpdatesName()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateListItemAsync(item.Id, "Bread", null, null, null, null);

        result.Name.Should().Be("Bread");
        result.Quantity.Should().Be(1m);
    }

    [Fact]
    public async Task UpdateListItemAsync_WhenQuantityProvided_UpdatesQuantity()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateListItemAsync(item.Id, null, 3.5m, null, null, null);

        result.Quantity.Should().Be(3.5m);
        result.Name.Should().Be("Milk");
    }

    [Fact]
    public async Task UpdateListItemAsync_WhenUnitProvided_UpdatesUnit()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, "gallon", 0);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateListItemAsync(item.Id, null, null, "liter", null, null);

        result.Unit.Should().Be("liter");
    }

    [Fact]
    public async Task UpdateListItemAsync_WhenIsCheckedProvided_UpdatesIsChecked()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateListItemAsync(item.Id, null, null, null, true, null);

        result.IsChecked.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateListItemAsync_WhenSortOrderProvided_UpdatesSortOrder()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateListItemAsync(item.Id, null, null, null, null, 5);

        result.SortOrder.Should().Be(5);
    }

    [Fact]
    public async Task UpdateListItemAsync_WhenAllFieldsNull_DoesNotChangeAnything()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, "gallon", 0);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateListItemAsync(item.Id, null, null, null, null, null);

        result.Name.Should().Be("Milk");
        result.Quantity.Should().Be(1m);
        result.Unit.Should().Be("gallon");
        result.IsChecked.Should().BeFalse();
        result.SortOrder.Should().Be(0);
    }

    [Fact]
    public async Task UpdateListItemAsync_WhenNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.UpdateListItemAsync(Guid.NewGuid(), "Nope", null, null, null, null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UpdateListItemAsync_WhenMultipleFieldsProvided_UpdatesAll()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, "gallon", 0);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateListItemAsync(item.Id, "Bread", 2.5m, "loaves", true, 3);

        result.Name.Should().Be("Bread");
        result.Quantity.Should().Be(2.5m);
        result.Unit.Should().Be("loaves");
        result.IsChecked.Should().BeTrue();
        result.SortOrder.Should().Be(3);
    }

    [Fact]
    public async Task UpdateListItemAsync_TrimsNameAndUnit()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateListItemAsync(item.Id, "  Bread  ", null, "  lbs  ", null, null);

        result.Name.Should().Be("Bread");
        result.Unit.Should().Be("lbs");
    }

    // ── Check/Uncheck Behavior ──────────────────────────────────────

    [Fact]
    public async Task UpdateListItemAsync_CheckItem_SetsIsCheckedTrue()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateListItemAsync(item.Id, null, null, null, true, null);

        result.IsChecked.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateListItemAsync_UncheckItem_SetsIsCheckedFalse()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);
        await _sut.UpdateListItemAsync(item.Id, null, null, null, true, null);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateListItemAsync(item.Id, null, null, null, false, null);

        result.IsChecked.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateListItemAsync_CheckAlreadyCheckedItem_RemainsChecked()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);
        await _sut.UpdateListItemAsync(item.Id, null, null, null, true, null);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateListItemAsync(item.Id, null, null, null, true, null);

        result.IsChecked.Should().BeTrue();
    }

    // ── Sort Order Reordering ───────────────────────────────────────

    [Fact]
    public async Task UpdateListItemAsync_ChangeSortOrder_UpdatesPosition()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateListItemAsync(item.Id, null, null, null, null, 2);

        result.SortOrder.Should().Be(2);
    }

    [Fact]
    public async Task CreateListItemAsync_MultipleSortOrders_CanHaveSameValue()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var item1 = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);
        var item2 = await _sut.CreateListItemAsync(list.Id, "Bread", 1m, null, 0);

        item1.SortOrder.Should().Be(0);
        item2.SortOrder.Should().Be(0);
    }

    [Fact]
    public async Task CreateListItemAsync_SequentialSortOrders_PreservesOrder()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        _context.ChangeTracker.Clear();

        var item0 = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);
        var item1 = await _sut.CreateListItemAsync(list.Id, "Bread", 1m, null, 1);
        var item2 = await _sut.CreateListItemAsync(list.Id, "Eggs", 1m, null, 2);
        _context.ChangeTracker.Clear();

        var items = await _context.Set<ListItem>()
            .Where(i => i.ShoppingListId == list.Id)
            .OrderBy(i => i.SortOrder)
            .ToListAsync();

        items.Should().HaveCount(3);
        items[0].Name.Should().Be("Milk");
        items[1].Name.Should().Be("Bread");
        items[2].Name.Should().Be("Eggs");
    }

    // ── Decimal Quantity Edge Cases ──────────────────────────────────

    [Fact]
    public async Task UpdateListItemAsync_QuantityToZero_Succeeds()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateListItemAsync(item.Id, null, 0m, null, null, null);

        result.Quantity.Should().Be(0m);
    }

    [Fact]
    public async Task UpdateListItemAsync_QuantityWithFourDecimalPlaces_PreservesPrecision()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item = await _sut.CreateListItemAsync(list.Id, "Spice", 1m, null, 0);
        _context.ChangeTracker.Clear();

        await _sut.UpdateListItemAsync(item.Id, null, 0.0001m, null, null, null);
        _context.ChangeTracker.Clear();

        var fetched = await _context.Set<ListItem>().FirstAsync(i => i.Id == item.Id);
        fetched.Quantity.Should().Be(0.0001m);
    }

    // ── DeleteListItemAsync ─────────────────────────────────────────

    [Fact]
    public async Task DeleteListItemAsync_WhenExists_ReturnsTrue()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);
        _context.ChangeTracker.Clear();

        var result = await _sut.DeleteListItemAsync(item.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteListItemAsync_WhenExists_SoftDeletesEntity()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);
        _context.ChangeTracker.Clear();

        await _sut.DeleteListItemAsync(item.Id);
        _context.ChangeTracker.Clear();

        var remaining = await _context.Set<ListItem>()
            .Where(i => i.ShoppingListId == list.Id)
            .ToListAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteListItemAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _sut.DeleteListItemAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── Clear Checked Items Pattern ─────────────────────────────────

    [Fact]
    public async Task DeleteListItemAsync_MultipleCheckedItems_CanBeDeletedIndependently()
    {
        var list = await _sut.CreateShoppingListAsync(_spaceId, "Groceries");
        var item1 = await _sut.CreateListItemAsync(list.Id, "Milk", 1m, null, 0);
        var item2 = await _sut.CreateListItemAsync(list.Id, "Bread", 1m, null, 1);
        var item3 = await _sut.CreateListItemAsync(list.Id, "Eggs", 1m, null, 2);

        // Check items 1 and 2
        await _sut.UpdateListItemAsync(item1.Id, null, null, null, true, null);
        await _sut.UpdateListItemAsync(item2.Id, null, null, null, true, null);
        _context.ChangeTracker.Clear();

        // Delete checked items
        await _sut.DeleteListItemAsync(item1.Id);
        await _sut.DeleteListItemAsync(item2.Id);
        _context.ChangeTracker.Clear();

        // Only unchecked item remains
        var remaining = await _context.Set<ListItem>()
            .Where(i => i.ShoppingListId == list.Id)
            .ToListAsync();

        remaining.Should().HaveCount(1);
        remaining[0].Name.Should().Be("Eggs");
        remaining[0].IsChecked.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
