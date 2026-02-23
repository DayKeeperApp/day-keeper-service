using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Infrastructure.Persistence.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Interceptors;

public sealed class AuditFieldsInterceptorTests : IDisposable
{
    private static readonly DateTime _fixedTime =
        new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly TestAuditDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuditFieldsInterceptorTests()
    {
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(_fixedTime);

        var interceptor = new AuditFieldsInterceptor(_dateTimeProvider);

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TestAuditDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(interceptor)
            .Options;

        _context = new TestAuditDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityAdded_SetsCreatedAtAndUpdatedAt()
    {
        var entity = new TestAuditEntity { Name = "test" };

        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();

        entity.CreatedAt.Should().Be(_fixedTime);
        entity.UpdatedAt.Should().Be(_fixedTime);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityModified_UpdatesOnlyUpdatedAt()
    {
        var entity = new TestAuditEntity { Name = "original" };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();

        var laterTime = _fixedTime.AddHours(1);
        _dateTimeProvider.UtcNow.Returns(laterTime);

        entity.Name = "updated";
        await _context.SaveChangesAsync();

        entity.CreatedAt.Should().Be(_fixedTime);
        entity.UpdatedAt.Should().Be(laterTime);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityNotModified_DoesNotChangeTimestamps()
    {
        var entity = new TestAuditEntity { Name = "stable" };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();

        var laterTime = _fixedTime.AddHours(2);
        _dateTimeProvider.UtcNow.Returns(laterTime);

        var retrieved = await _context.TestEntities.FindAsync(entity.Id);

        retrieved!.CreatedAt.Should().Be(_fixedTime);
        retrieved.UpdatedAt.Should().Be(_fixedTime);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private sealed class TestAuditEntity : BaseEntity
    {
        public required string Name { get; set; }
    }

    private sealed class TestAuditDbContext(
        DbContextOptions<TestAuditDbContext> options) : DbContext(options)
    {
        public DbSet<TestAuditEntity> TestEntities => Set<TestAuditEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAuditEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.Property(e => e.DeletedAt).IsRequired(false);
                entity.Ignore(e => e.IsDeleted);
                entity.Property(e => e.Name).IsRequired();
            });
        }
    }
}
