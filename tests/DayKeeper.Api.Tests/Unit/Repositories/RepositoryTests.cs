using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence.Interceptors;
using DayKeeper.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Repositories;

public sealed class RepositoryTests : IDisposable
{
    private static readonly DateTime _fixedTime =
        new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly TestRepositoryDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly Repository<TestEntity> _sut;

    public RepositoryTests()
    {
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(_fixedTime);

        var interceptor = new AuditFieldsInterceptor(_dateTimeProvider);

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TestRepositoryDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(interceptor)
            .Options;

        _context = new TestRepositoryDbContext(options);
        _context.Database.EnsureCreated();

        _sut = new Repository<TestEntity>(_context, _dateTimeProvider);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityExists_ReturnsEntity()
    {
        var entity = new TestEntity { Name = "test" };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(entity.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Name.Should().Be("test");
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityIsSoftDeleted_ReturnsNull()
    {
        var entity = new TestEntity { Name = "deleted" };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();

        entity.DeletedAt = _fixedTime;
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var result = await _sut.GetByIdAsync(entity.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WhenEntitiesExist_ReturnsAllEntities()
    {
        _context.TestEntities.Add(new TestEntity { Name = "first" });
        _context.TestEntities.Add(new TestEntity { Name = "second" });
        await _context.SaveChangesAsync();

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoEntitiesExist_ReturnsEmptyList()
    {
        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WhenSomeEntitiesAreSoftDeleted_ExcludesSoftDeleted()
    {
        _context.TestEntities.Add(new TestEntity { Name = "active" });
        var deleted = new TestEntity { Name = "deleted" };
        _context.TestEntities.Add(deleted);
        await _context.SaveChangesAsync();

        deleted.DeletedAt = _fixedTime;
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var result = await _sut.GetAllAsync();

        result.Should().ContainSingle()
            .Which.Name.Should().Be("active");
    }

    [Fact]
    public async Task AddAsync_WhenCalled_PersistsEntityAndReturnsIt()
    {
        var entity = new TestEntity { Name = "new-entity" };

        var result = await _sut.AddAsync(entity);

        result.Should().BeSameAs(entity);

        _context.ChangeTracker.Clear();

        var persisted = await _context.TestEntities
            .FirstOrDefaultAsync(e => e.Id == entity.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("new-entity");
    }

    [Fact]
    public async Task UpdateAsync_WhenCalled_PersistsChanges()
    {
        var entity = new TestEntity { Name = "original" };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();

        entity.Name = "updated";
        await _sut.UpdateAsync(entity);

        _context.ChangeTracker.Clear();

        var persisted = await _context.TestEntities
            .FirstOrDefaultAsync(e => e.Id == entity.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("updated");
    }

    [Fact]
    public async Task DeleteAsync_WhenEntityExists_SoftDeletesAndReturnsTrue()
    {
        var entity = new TestEntity { Name = "to-delete" };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();

        var result = await _sut.DeleteAsync(entity.Id);

        result.Should().BeTrue();

        var deleted = await _context.TestEntities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entity.Id);
        deleted.Should().NotBeNull();
        deleted!.DeletedAt.Should().Be(_fixedTime);
    }

    [Fact]
    public async Task DeleteAsync_WhenEntityDoesNotExist_ReturnsFalse()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private sealed class TestEntity : BaseEntity
    {
        public required string Name { get; set; }
    }

    private sealed class TestRepositoryDbContext(
        DbContextOptions<TestRepositoryDbContext> options) : DbContext(options)
    {
        public DbSet<TestEntity> TestEntities => Set<TestEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.Property(e => e.DeletedAt).IsRequired(false);
                entity.Ignore(e => e.IsDeleted);
                entity.Property(e => e.Name).IsRequired();
                entity.HasQueryFilter(e => e.DeletedAt == null);
            });
        }
    }
}
