using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Persistence.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Configurations;

public sealed class ImportantDateConfigurationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ImportantDateTestDbContext _context;

    public ImportantDateConfigurationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ImportantDateTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ImportantDateTestDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public void Model_HasIndexOnPersonId()
    {
        var entityType = _context.Model.FindEntityType(typeof(ImportantDate))!;
        var index = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 1
                && string.Equals(i.Properties[0].Name, nameof(ImportantDate.PersonId), StringComparison.Ordinal));

        index.Should().NotBeNull();
    }

    [Fact]
    public void Model_HasIndexOnEventTypeId()
    {
        var entityType = _context.Model.FindEntityType(typeof(ImportantDate))!;
        var index = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 1
                && string.Equals(i.Properties[0].Name, nameof(ImportantDate.EventTypeId), StringComparison.Ordinal));

        index.Should().NotBeNull();
    }

    [Fact]
    public async Task Person_CascadeDelete_RemovesImportantDates()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var space = CreateSpace(tenant.Id);
        _context.Spaces.Add(space);
        await _context.SaveChangesAsync();

        var person = CreatePerson(space.Id);
        _context.People.Add(person);
        await _context.SaveChangesAsync();

        _context.ImportantDates.Add(new ImportantDate
        {
            PersonId = person.Id,
            Label = "Birthday",
            Date = new DateOnly(1990, 6, 15),
        });
        _context.ImportantDates.Add(new ImportantDate
        {
            PersonId = person.Id,
            Label = "Anniversary",
            Date = new DateOnly(2015, 9, 20),
        });
        await _context.SaveChangesAsync();

        _context.People.Remove(person);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var remaining = await _context.ImportantDates.ToListAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task EventType_Delete_SetsEventTypeIdToNull()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var space = CreateSpace(tenant.Id);
        _context.Spaces.Add(space);
        var eventType = new EventType
        {
            Name = "Birthday",
            NormalizedName = "birthday",
            Color = "#4A90D9",
        };
        _context.EventTypes.Add(eventType);
        await _context.SaveChangesAsync();

        var person = CreatePerson(space.Id);
        _context.People.Add(person);
        await _context.SaveChangesAsync();

        var importantDate = new ImportantDate
        {
            PersonId = person.Id,
            Label = "Birthday",
            Date = new DateOnly(1990, 6, 15),
            EventTypeId = eventType.Id,
        };
        _context.ImportantDates.Add(importantDate);
        await _context.SaveChangesAsync();

        _context.EventTypes.Remove(eventType);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var reloaded = await _context.ImportantDates.SingleAsync();
        reloaded.EventTypeId.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static Space CreateSpace(Guid tenantId) => new()
    {
        TenantId = tenantId,
        Name = "Test Space",
        NormalizedName = "test space",
        SpaceType = SpaceType.Shared,
    };

    private static Person CreatePerson(Guid spaceId) => new()
    {
        SpaceId = spaceId,
        FirstName = "Alice",
        LastName = "Smith",
        NormalizedFullName = "alice smith",
    };

    private sealed class ImportantDateTestDbContext(
        DbContextOptions<ImportantDateTestDbContext> options) : DbContext(options)
    {
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Space> Spaces => Set<Space>();
        public DbSet<Person> People => Set<Person>();
        public DbSet<EventType> EventTypes => Set<EventType>();
        public DbSet<ImportantDate> ImportantDates => Set<ImportantDate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new TenantConfiguration().Configure(modelBuilder.Entity<Tenant>());
            new SpaceConfiguration().Configure(modelBuilder.Entity<Space>());
            new PersonConfiguration().Configure(modelBuilder.Entity<Person>());
            new ImportantDateConfiguration().Configure(modelBuilder.Entity<ImportantDate>());

            // Minimal EventType configuration for test purposes
            // (no EventTypeConfiguration exists in the project yet)
            modelBuilder.Entity<EventType>(b =>
            {
                b.HasKey(e => e.Id);
                b.Property(e => e.Id).ValueGeneratedNever();
                b.Property(e => e.Name).IsRequired();
                b.Property(e => e.NormalizedName).IsRequired();
                b.Property(e => e.Color).IsRequired();
                b.Ignore(e => e.IsSystem);
                b.Ignore(e => e.Events);
            });
        }
    }
}
