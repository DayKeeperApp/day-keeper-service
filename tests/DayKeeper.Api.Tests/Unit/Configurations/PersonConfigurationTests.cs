using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Persistence.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Configurations;

public sealed class PersonConfigurationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PersonTestDbContext _context;

    public PersonConfigurationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PersonTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new PersonTestDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task SpaceIdAndNormalizedFullName_UniqueIndex_RejectsDuplicate()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var space = CreateSpace(tenant.Id);
        _context.Spaces.Add(space);
        await _context.SaveChangesAsync();

        _context.People.Add(CreatePerson(space.Id, "Alice", "Smith", "alice smith"));
        await _context.SaveChangesAsync();

        _context.People.Add(CreatePerson(space.Id, "Alice", "Smith", "alice smith"));

        var act = () => _context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SpaceIdAndNormalizedFullName_UniqueIndex_AllowsSameNameDifferentSpace()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var spaceA = CreateSpace(tenant.Id, "Family", "family");
        var spaceB = CreateSpace(tenant.Id, "Work", "work");
        _context.Spaces.AddRange(spaceA, spaceB);
        await _context.SaveChangesAsync();

        _context.People.Add(CreatePerson(spaceA.Id, "Alice", "Smith", "alice smith"));
        _context.People.Add(CreatePerson(spaceB.Id, "Alice", "Smith", "alice smith"));

        var act = () => _context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Model_HasUniqueCompositeIndexOnSpaceIdAndNormalizedFullName()
    {
        var entityType = _context.Model.FindEntityType(typeof(Person))!;
        var compositeIndex = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 2
                && string.Equals(i.Properties[0].Name, nameof(Person.SpaceId), StringComparison.Ordinal)
                && string.Equals(i.Properties[1].Name, nameof(Person.NormalizedFullName), StringComparison.Ordinal));

        compositeIndex.Should().NotBeNull();
        compositeIndex!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task Space_CascadeDelete_RemovesPeople()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var space = CreateSpace(tenant.Id);
        _context.Spaces.Add(space);
        await _context.SaveChangesAsync();

        _context.People.Add(CreatePerson(space.Id, "Alice", "Smith", "alice smith"));
        _context.People.Add(CreatePerson(space.Id, "Bob", "Jones", "bob jones"));
        await _context.SaveChangesAsync();

        _context.Spaces.Remove(space);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var remaining = await _context.People.ToListAsync();
        remaining.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static Space CreateSpace(Guid tenantId, string name = "Test Space", string normalizedName = "test space") => new()
    {
        TenantId = tenantId,
        Name = name,
        NormalizedName = normalizedName,
        SpaceType = SpaceType.Shared,
    };

    private static Person CreatePerson(Guid spaceId, string firstName, string lastName, string normalizedFullName) => new()
    {
        SpaceId = spaceId,
        FirstName = firstName,
        LastName = lastName,
        NormalizedFullName = normalizedFullName,
    };

    private sealed class PersonTestDbContext(
        DbContextOptions<PersonTestDbContext> options) : DbContext(options)
    {
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Space> Spaces => Set<Space>();
        public DbSet<Person> People => Set<Person>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new TenantConfiguration().Configure(modelBuilder.Entity<Tenant>());
            new SpaceConfiguration().Configure(modelBuilder.Entity<Space>());
            new PersonConfiguration().Configure(modelBuilder.Entity<Person>());
        }
    }
}
