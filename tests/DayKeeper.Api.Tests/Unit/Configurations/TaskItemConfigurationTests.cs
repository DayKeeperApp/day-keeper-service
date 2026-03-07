using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Persistence.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Configurations;

public sealed class TaskItemConfigurationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TaskItemTestDbContext _context;

    public TaskItemConfigurationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TaskItemTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TaskItemTestDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task TaskCategory_UniqueIndex_RejectsDuplicate()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var space = CreateSpace(tenant.Id);
        _context.Spaces.Add(space);
        await _context.SaveChangesAsync();

        var category = CreateCategory(tenant.Id);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var task = CreateTaskItem(space.Id);
        _context.TaskItems.Add(task);
        await _context.SaveChangesAsync();

        _context.TaskCategories.Add(new TaskCategory
        {
            TaskItemId = task.Id,
            CategoryId = category.Id,
        });
        await _context.SaveChangesAsync();

        _context.TaskCategories.Add(new TaskCategory
        {
            TaskItemId = task.Id,
            CategoryId = category.Id,
        });

        var act = () => _context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task TaskCategory_UniqueIndex_AllowsSameCategoryOnDifferentTasks()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var space = CreateSpace(tenant.Id);
        _context.Spaces.Add(space);
        await _context.SaveChangesAsync();

        var category = CreateCategory(tenant.Id);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var taskA = CreateTaskItem(space.Id);
        var taskB = CreateTaskItem(space.Id);
        _context.TaskItems.AddRange(taskA, taskB);
        await _context.SaveChangesAsync();

        _context.TaskCategories.Add(new TaskCategory
        {
            TaskItemId = taskA.Id,
            CategoryId = category.Id,
        });
        _context.TaskCategories.Add(new TaskCategory
        {
            TaskItemId = taskB.Id,
            CategoryId = category.Id,
        });

        var act = () => _context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Space_CascadeDelete_RemovesTaskItems()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var space = CreateSpace(tenant.Id);
        _context.Spaces.Add(space);
        await _context.SaveChangesAsync();

        _context.TaskItems.Add(CreateTaskItem(space.Id));
        _context.TaskItems.Add(CreateTaskItem(space.Id));
        await _context.SaveChangesAsync();

        _context.Spaces.Remove(space);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var remaining = await _context.TaskItems.ToListAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task TaskItem_CascadeDelete_RemovesTaskCategories()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var space = CreateSpace(tenant.Id);
        _context.Spaces.Add(space);
        await _context.SaveChangesAsync();

        var category = CreateCategory(tenant.Id);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var task = CreateTaskItem(space.Id);
        _context.TaskItems.Add(task);
        await _context.SaveChangesAsync();

        _context.TaskCategories.Add(new TaskCategory
        {
            TaskItemId = task.Id,
            CategoryId = category.Id,
        });
        await _context.SaveChangesAsync();

        _context.TaskItems.Remove(task);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var remaining = await _context.TaskCategories.ToListAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public void Model_HasCompositeIndexOnSpaceIdStatusDueAt()
    {
        var entityType = _context.Model.FindEntityType(typeof(TaskItem))!;
        var index = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 3
                && string.Equals(i.Properties[0].Name, nameof(TaskItem.SpaceId), StringComparison.Ordinal)
                && string.Equals(i.Properties[1].Name, nameof(TaskItem.Status), StringComparison.Ordinal)
                && string.Equals(i.Properties[2].Name, nameof(TaskItem.DueAt), StringComparison.Ordinal));

        index.Should().NotBeNull();
    }

    [Fact]
    public void Model_HasCompositeIndexOnSpaceIdStatusDueDate()
    {
        var entityType = _context.Model.FindEntityType(typeof(TaskItem))!;
        var index = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 3
                && string.Equals(i.Properties[0].Name, nameof(TaskItem.SpaceId), StringComparison.Ordinal)
                && string.Equals(i.Properties[1].Name, nameof(TaskItem.Status), StringComparison.Ordinal)
                && string.Equals(i.Properties[2].Name, nameof(TaskItem.DueDate), StringComparison.Ordinal));

        index.Should().NotBeNull();
    }

    [Fact]
    public void Model_HasCompositeIndexOnSpaceIdAndUpdatedAt()
    {
        var entityType = _context.Model.FindEntityType(typeof(TaskItem))!;
        var index = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 2
                && string.Equals(i.Properties[0].Name, nameof(TaskItem.SpaceId), StringComparison.Ordinal)
                && string.Equals(i.Properties[1].Name, nameof(TaskItem.UpdatedAt), StringComparison.Ordinal));

        index.Should().NotBeNull();
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

    private static TaskItem CreateTaskItem(Guid spaceId) => new()
    {
        SpaceId = spaceId,
        Title = $"Task-{Guid.NewGuid():N}",
        Status = TaskItemStatus.Open,
        Priority = TaskItemPriority.None,
    };

    private static Category CreateCategory(Guid tenantId) => new()
    {
        TenantId = tenantId,
        Name = $"Cat-{Guid.NewGuid():N}",
        NormalizedName = $"cat-{Guid.NewGuid():N}",
        Color = "#FF0000",
    };

    private sealed class TaskItemTestDbContext(
        DbContextOptions<TaskItemTestDbContext> options) : DbContext(options)
    {
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Space> Spaces => Set<Space>();
        public DbSet<TaskItem> TaskItems => Set<TaskItem>();
        public DbSet<TaskCategory> TaskCategories => Set<TaskCategory>();
        public DbSet<Category> Categories => Set<Category>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new TenantConfiguration().Configure(modelBuilder.Entity<Tenant>());
            new SpaceConfiguration().Configure(modelBuilder.Entity<Space>());
            new TaskItemConfiguration().Configure(modelBuilder.Entity<TaskItem>());
            new TaskCategoryConfiguration().Configure(modelBuilder.Entity<TaskCategory>());
            new CategoryConfiguration().Configure(modelBuilder.Entity<Category>());
        }
    }
}
