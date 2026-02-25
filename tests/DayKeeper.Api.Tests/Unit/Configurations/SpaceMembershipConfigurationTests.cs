using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Persistence.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Configurations;

public sealed class SpaceMembershipConfigurationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly MembershipTestDbContext _context;

    public SpaceMembershipConfigurationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<MembershipTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new MembershipTestDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task SpaceIdAndUserId_UniqueIndex_RejectsDuplicate()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var user = CreateUser(tenant.Id);
        _context.Users.Add(user);
        var space = CreateSpace(tenant.Id);
        _context.Spaces.Add(space);
        await _context.SaveChangesAsync();

        _context.Memberships.Add(CreateMembership(space.Id, user.Id));
        await _context.SaveChangesAsync();

        _context.Memberships.Add(CreateMembership(space.Id, user.Id));

        var act = () => _context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SpaceIdAndUserId_UniqueIndex_AllowsSameUserDifferentSpaces()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var user = CreateUser(tenant.Id);
        _context.Users.Add(user);
        var spaceA = CreateSpace(tenant.Id, "Family", "family");
        var spaceB = CreateSpace(tenant.Id, "Work", "work");
        _context.Spaces.AddRange(spaceA, spaceB);
        await _context.SaveChangesAsync();

        _context.Memberships.Add(CreateMembership(spaceA.Id, user.Id));
        _context.Memberships.Add(CreateMembership(spaceB.Id, user.Id));

        var act = () => _context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Model_HasUniqueCompositeIndexOnSpaceIdAndUserId()
    {
        var entityType = _context.Model.FindEntityType(typeof(SpaceMembership))!;
        var compositeIndex = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 2
                && string.Equals(i.Properties[0].Name, nameof(SpaceMembership.SpaceId), StringComparison.Ordinal)
                && string.Equals(i.Properties[1].Name, nameof(SpaceMembership.UserId), StringComparison.Ordinal));

        compositeIndex.Should().NotBeNull();
        compositeIndex!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public void Model_HasIndexOnUserId()
    {
        var entityType = _context.Model.FindEntityType(typeof(SpaceMembership))!;
        var userIdIndex = entityType.GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 1
                && string.Equals(i.Properties[0].Name, nameof(SpaceMembership.UserId), StringComparison.Ordinal));

        userIdIndex.Should().NotBeNull();
    }

    [Fact]
    public async Task Space_CascadeDelete_RemovesMemberships()
    {
        var tenant = new Tenant { Name = "Acme", Slug = "acme" };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var userA = CreateUser(tenant.Id, "alice@example.com");
        var userB = CreateUser(tenant.Id, "bob@example.com");
        _context.Users.AddRange(userA, userB);
        var space = CreateSpace(tenant.Id);
        _context.Spaces.Add(space);
        await _context.SaveChangesAsync();

        _context.Memberships.Add(CreateMembership(space.Id, userA.Id));
        _context.Memberships.Add(CreateMembership(space.Id, userB.Id));
        await _context.SaveChangesAsync();

        _context.Spaces.Remove(space);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var remaining = await _context.Memberships.ToListAsync();
        remaining.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static User CreateUser(Guid tenantId, string email = "test@example.com") => new()
    {
        TenantId = tenantId,
        DisplayName = "Test User",
        Email = email,
        Timezone = "America/Chicago",
    };

    private static Space CreateSpace(Guid tenantId, string name = "Test Space", string normalizedName = "test space") => new()
    {
        TenantId = tenantId,
        Name = name,
        NormalizedName = normalizedName,
        SpaceType = SpaceType.Shared,
    };

    private static SpaceMembership CreateMembership(Guid spaceId, Guid userId) => new()
    {
        SpaceId = spaceId,
        UserId = userId,
        Role = SpaceRole.Editor,
    };

    private sealed class MembershipTestDbContext(
        DbContextOptions<MembershipTestDbContext> options) : DbContext(options)
    {
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Space> Spaces => Set<Space>();
        public DbSet<SpaceMembership> Memberships => Set<SpaceMembership>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new TenantConfiguration().Configure(modelBuilder.Entity<Tenant>());
            new UserConfiguration().Configure(modelBuilder.Entity<User>());
            new SpaceConfiguration().Configure(modelBuilder.Entity<Space>());
            new SpaceMembershipConfiguration().Configure(modelBuilder.Entity<SpaceMembership>());
        }
    }
}
