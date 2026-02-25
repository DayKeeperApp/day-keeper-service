using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Persistence.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Configurations;

public sealed class ContactMethodConfigurationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ContactMethodTestDbContext _context;

    public ContactMethodConfigurationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ContactMethodTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ContactMethodTestDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public void Model_HasIndexOnPersonId()
    {
        var entityType = _context.Model.FindEntityType(typeof(ContactMethod))!;
        var index = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 1
                && string.Equals(i.Properties[0].Name, nameof(ContactMethod.PersonId), StringComparison.Ordinal));

        index.Should().NotBeNull();
    }

    [Fact]
    public async Task Person_CascadeDelete_RemovesContactMethods()
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

        _context.ContactMethods.Add(new ContactMethod
        {
            PersonId = person.Id,
            Type = ContactMethodType.Email,
            Value = "alice@example.com",
        });
        _context.ContactMethods.Add(new ContactMethod
        {
            PersonId = person.Id,
            Type = ContactMethodType.Phone,
            Value = "+1-555-0100",
        });
        await _context.SaveChangesAsync();

        _context.People.Remove(person);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var remaining = await _context.ContactMethods.ToListAsync();
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

    private sealed class ContactMethodTestDbContext(
        DbContextOptions<ContactMethodTestDbContext> options) : DbContext(options)
    {
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Space> Spaces => Set<Space>();
        public DbSet<Person> People => Set<Person>();
        public DbSet<ContactMethod> ContactMethods => Set<ContactMethod>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new TenantConfiguration().Configure(modelBuilder.Entity<Tenant>());
            new SpaceConfiguration().Configure(modelBuilder.Entity<Space>());
            new PersonConfiguration().Configure(modelBuilder.Entity<Person>());
            new ContactMethodConfiguration().Configure(modelBuilder.Entity<ContactMethod>());
        }
    }
}
