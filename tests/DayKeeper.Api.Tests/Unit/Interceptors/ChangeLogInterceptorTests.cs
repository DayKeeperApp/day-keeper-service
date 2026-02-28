using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Persistence.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Interceptors;

public sealed class ChangeLogInterceptorTests : IDisposable
{
    private static readonly DateTime _fixedTime =
        new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _spaceId = Guid.NewGuid();

    private readonly SqliteConnection _connection;
    private readonly TestChangeLogDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChangeLogInterceptorTests()
    {
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(_fixedTime);

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.CurrentTenantId.Returns(_tenantId);

        var auditInterceptor = new AuditFieldsInterceptor(_dateTimeProvider);
        var changeLogInterceptor = new ChangeLogInterceptor(
            _dateTimeProvider, tenantContext);

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TestChangeLogDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(auditInterceptor, changeLogInterceptor)
            .Options;

        _context = new TestChangeLogDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityAdded_CreatesCreatedEntry()
    {
        _context.Set<User>().Add(CreateUser());
        await _context.SaveChangesAsync();

        var log = await _context.Set<ChangeLog>().SingleAsync();
        log.Operation.Should().Be(ChangeOperation.Created);
        log.EntityType.Should().Be(ChangeLogEntityType.User);
        log.Timestamp.Should().Be(_fixedTime);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEntityModified_CreatesUpdatedEntry()
    {
        var user = CreateUser();
        _context.Set<User>().Add(user);
        await _context.SaveChangesAsync();
        ClearChangeLogs();

        user.DisplayName = "Updated";
        await _context.SaveChangesAsync();

        var log = await _context.Set<ChangeLog>().SingleAsync();
        log.Operation.Should().Be(ChangeOperation.Updated);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenSoftDeleted_CreatesDeletedEntry()
    {
        var user = CreateUser();
        _context.Set<User>().Add(user);
        await _context.SaveChangesAsync();
        ClearChangeLogs();

        var laterTime = _fixedTime.AddHours(1);
        _dateTimeProvider.UtcNow.Returns(laterTime);

        user.DeletedAt = laterTime;
        await _context.SaveChangesAsync();

        var log = await _context.Set<ChangeLog>().SingleAsync();
        log.Operation.Should().Be(ChangeOperation.Deleted);
    }

    [Fact]
    public async Task SaveChangesAsync_SetsTenantIdFromITenantScoped()
    {
        _context.Set<User>().Add(CreateUser());
        await _context.SaveChangesAsync();

        var log = await _context.Set<ChangeLog>().SingleAsync();
        log.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public async Task SaveChangesAsync_SetsSpaceIdForCalendar()
    {
        _context.Set<Calendar>().Add(CreateCalendar());
        await _context.SaveChangesAsync();

        var log = await _context.Set<ChangeLog>().SingleAsync();
        log.SpaceId.Should().Be(_spaceId);
    }

    [Fact]
    public async Task SaveChangesAsync_SetsNullSpaceId_ForNonSpaceScopedEntity()
    {
        _context.Set<User>().Add(CreateUser());
        await _context.SaveChangesAsync();

        var log = await _context.Set<ChangeLog>().SingleAsync();
        log.SpaceId.Should().BeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenNoTrackedChanges_CreatesNoEntries()
    {
        await _context.SaveChangesAsync();

        var logs = await _context.Set<ChangeLog>().ToListAsync();
        logs.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenMultipleEntities_CreatesMultipleEntries()
    {
        _context.Set<User>().Add(CreateUser());
        _context.Set<Calendar>().Add(CreateCalendar());
        await _context.SaveChangesAsync();

        var logs = await _context.Set<ChangeLog>().ToListAsync();
        logs.Should().HaveCount(2);
    }

    [Fact]
    public async Task SaveChangesAsync_SetsCorrectEntityId()
    {
        var user = CreateUser();
        _context.Set<User>().Add(user);
        await _context.SaveChangesAsync();

        var log = await _context.Set<ChangeLog>().SingleAsync();
        log.EntityId.Should().Be(user.Id);
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private void ClearChangeLogs()
    {
        _context.Set<ChangeLog>().RemoveRange(_context.Set<ChangeLog>());
        _context.SaveChanges();
    }

    private static User CreateUser() => new()
    {
        TenantId = _tenantId,
        DisplayName = "Test User",
        Email = $"test-{Guid.NewGuid():N}@example.com",
        Timezone = "UTC",
        WeekStart = WeekStart.Monday,
    };

    private static Calendar CreateCalendar() => new()
    {
        SpaceId = _spaceId,
        Name = "Test Calendar",
        NormalizedName = "test calendar",
        Color = "#FF0000",
    };

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // ── Test DbContext ────────────────────────────────────────────────

    private sealed class TestChangeLogDbContext(
        DbContextOptions<TestChangeLogDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.TenantId).IsRequired();
                entity.Property(e => e.DisplayName).IsRequired();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.Timezone).IsRequired();
                entity.Property(e => e.WeekStart)
                    .HasConversion<string>().HasMaxLength(16);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.Property(e => e.DeletedAt).IsRequired(false);
                entity.Ignore(e => e.IsDeleted);
                entity.Ignore(e => e.Tenant);
                entity.Ignore(e => e.SpaceMemberships);
            });

            modelBuilder.Entity<Calendar>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.SpaceId).IsRequired();
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.NormalizedName).IsRequired();
                entity.Property(e => e.Color).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.Property(e => e.DeletedAt).IsRequired(false);
                entity.Ignore(e => e.IsDeleted);
                entity.Ignore(e => e.Space);
                entity.Ignore(e => e.Events);
            });

            modelBuilder.Entity<ChangeLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.EntityType)
                    .IsRequired().HasConversion<string>().HasMaxLength(32);
                entity.Property(e => e.EntityId).IsRequired();
                entity.Property(e => e.Operation)
                    .IsRequired().HasConversion<string>().HasMaxLength(16);
                entity.Property(e => e.TenantId).IsRequired(false);
                entity.Property(e => e.SpaceId).IsRequired(false);
                entity.Property(e => e.Timestamp).IsRequired();
            });
        }
    }
}
