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

public sealed class SpaceServiceTests : IDisposable
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _ownerUserId = Guid.NewGuid();
    private static readonly Guid _memberUserId = Guid.NewGuid();
    private static readonly Guid _existingSpaceId = Guid.NewGuid();
    private static readonly Guid _ownerMembershipId = Guid.NewGuid();
    private static readonly DateTime _fixedTime =
        new(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DayKeeperDbContext _context;
    private readonly SpaceService _sut;

    public SpaceServiceTests()
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

        var spaceRepository = new Repository<Space>(_context, dateTimeProvider);
        var membershipRepository = new Repository<SpaceMembership>(_context, dateTimeProvider);
        var userRepository = new Repository<User>(_context, dateTimeProvider);

        SeedData();

        _sut = new SpaceService(spaceRepository, membershipRepository, userRepository, _context);
    }

    private void SeedData()
    {
        _context.Set<Tenant>().Add(new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
        });
        _context.Set<User>().AddRange(
            new User
            {
                Id = _ownerUserId,
                TenantId = _tenantId,
                DisplayName = "Owner",
                Email = "owner@test.com",
                Timezone = "UTC",
                WeekStart = WeekStart.Monday,
            },
            new User
            {
                Id = _memberUserId,
                TenantId = _tenantId,
                DisplayName = "Member",
                Email = "member@test.com",
                Timezone = "UTC",
                WeekStart = WeekStart.Monday,
            });
        _context.Set<Space>().Add(new Space
        {
            Id = _existingSpaceId,
            TenantId = _tenantId,
            Name = "Existing Space",
            NormalizedName = "existing space",
            SpaceType = SpaceType.Shared,
        });
        _context.Set<SpaceMembership>().Add(new SpaceMembership
        {
            Id = _ownerMembershipId,
            SpaceId = _existingSpaceId,
            UserId = _ownerUserId,
            Role = SpaceRole.Owner,
        });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    // ── CreateSpaceAsync ────────────────────────────────────────────

    [Fact]
    public async Task CreateSpaceAsync_WhenValid_ReturnsSpace()
    {
        var result = await _sut.CreateSpaceAsync(
            _tenantId, "New Space", SpaceType.Personal, _ownerUserId);

        result.Should().NotBeNull();
        result.Name.Should().Be("New Space");
        result.SpaceType.Should().Be(SpaceType.Personal);
        result.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public async Task CreateSpaceAsync_WhenValid_CreatesOwnerMembership()
    {
        var result = await _sut.CreateSpaceAsync(
            _tenantId, "With Owner", SpaceType.Shared, _ownerUserId);

        _context.ChangeTracker.Clear();
        var membership = await _context.Set<SpaceMembership>()
            .FirstOrDefaultAsync(m => m.SpaceId == result.Id && m.UserId == _ownerUserId);

        membership.Should().NotBeNull();
        membership!.Role.Should().Be(SpaceRole.Owner);
    }

    [Fact]
    public async Task CreateSpaceAsync_WhenUserNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CreateSpaceAsync(
            _tenantId, "Orphan Space", SpaceType.Personal, Guid.NewGuid());

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CreateSpaceAsync_WhenDuplicateName_ThrowsDuplicateSpaceNameException()
    {
        var act = () => _sut.CreateSpaceAsync(
            _tenantId, "Existing Space", SpaceType.Shared, _ownerUserId);

        await act.Should().ThrowAsync<DuplicateSpaceNameException>();
    }

    [Fact]
    public async Task CreateSpaceAsync_NormalizesNameToLowercase()
    {
        var result = await _sut.CreateSpaceAsync(
            _tenantId, "  My SPACE  ", SpaceType.Personal, _ownerUserId);

        result.Name.Should().Be("My SPACE");
        result.NormalizedName.Should().Be("my space");
    }

    // ── GetSpaceAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetSpaceAsync_WhenSpaceExists_ReturnsSpace()
    {
        var result = await _sut.GetSpaceAsync(_existingSpaceId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(_existingSpaceId);
        result.Name.Should().Be("Existing Space");
    }

    [Fact]
    public async Task GetSpaceAsync_WhenSpaceDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetSpaceAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── UpdateSpaceAsync ────────────────────────────────────────────

    [Fact]
    public async Task UpdateSpaceAsync_WhenNameProvided_UpdatesName()
    {
        var result = await _sut.UpdateSpaceAsync(_existingSpaceId, "Updated Space", null);

        result.Name.Should().Be("Updated Space");
        result.NormalizedName.Should().Be("updated space");
    }

    [Fact]
    public async Task UpdateSpaceAsync_WhenSpaceTypeProvided_UpdatesSpaceType()
    {
        var result = await _sut.UpdateSpaceAsync(_existingSpaceId, null, SpaceType.System);

        result.SpaceType.Should().Be(SpaceType.System);
        result.Name.Should().Be("Existing Space");
    }

    [Fact]
    public async Task UpdateSpaceAsync_WhenSpaceDoesNotExist_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.UpdateSpaceAsync(Guid.NewGuid(), "Name", null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UpdateSpaceAsync_WhenNewNameConflicts_ThrowsDuplicateSpaceNameException()
    {
        // Create a second space to conflict with
        await _sut.CreateSpaceAsync(_tenantId, "Second Space", SpaceType.Shared, _ownerUserId);

        var act = () => _sut.UpdateSpaceAsync(_existingSpaceId, "Second Space", null);

        await act.Should().ThrowAsync<DuplicateSpaceNameException>();
    }

    [Fact]
    public async Task UpdateSpaceAsync_WhenSameNameProvided_DoesNotThrow()
    {
        var act = () => _sut.UpdateSpaceAsync(_existingSpaceId, "Existing Space", null);

        await act.Should().NotThrowAsync();
    }

    // ── DeleteSpaceAsync ────────────────────────────────────────────

    [Fact]
    public async Task DeleteSpaceAsync_WhenSpaceExists_DeletesMembershipsAndSpace()
    {
        var result = await _sut.DeleteSpaceAsync(_existingSpaceId);

        result.Should().BeTrue();

        _context.ChangeTracker.Clear();

        // Space should be soft-deleted (filtered out by query filter)
        var space = await _context.Set<Space>()
            .FirstOrDefaultAsync(s => s.Id == _existingSpaceId);
        space.Should().BeNull();

        // Membership should be soft-deleted too
        var membership = await _context.Set<SpaceMembership>()
            .FirstOrDefaultAsync(m => m.Id == _ownerMembershipId);
        membership.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSpaceAsync_WhenSpaceDoesNotExist_ReturnsFalse()
    {
        var result = await _sut.DeleteSpaceAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── AddMemberAsync ──────────────────────────────────────────────

    [Fact]
    public async Task AddMemberAsync_WhenValid_ReturnsMembership()
    {
        var result = await _sut.AddMemberAsync(_existingSpaceId, _memberUserId, SpaceRole.Editor);

        result.Should().NotBeNull();
        result.SpaceId.Should().Be(_existingSpaceId);
        result.UserId.Should().Be(_memberUserId);
        result.Role.Should().Be(SpaceRole.Editor);
    }

    [Fact]
    public async Task AddMemberAsync_WhenSpaceNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.AddMemberAsync(Guid.NewGuid(), _memberUserId, SpaceRole.Viewer);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task AddMemberAsync_WhenUserNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.AddMemberAsync(_existingSpaceId, Guid.NewGuid(), SpaceRole.Viewer);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task AddMemberAsync_WhenDuplicateMembership_ThrowsDuplicateMembershipException()
    {
        var act = () => _sut.AddMemberAsync(_existingSpaceId, _ownerUserId, SpaceRole.Editor);

        await act.Should().ThrowAsync<DuplicateMembershipException>();
    }

    // ── RemoveMemberAsync ───────────────────────────────────────────

    [Fact]
    public async Task RemoveMemberAsync_WhenValid_ReturnsTrue()
    {
        // Add a second member so we can remove one without hitting last-owner guard
        await _sut.AddMemberAsync(_existingSpaceId, _memberUserId, SpaceRole.Editor);

        var result = await _sut.RemoveMemberAsync(_existingSpaceId, _memberUserId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenLastOwner_ThrowsBusinessRuleViolationException()
    {
        var act = () => _sut.RemoveMemberAsync(_existingSpaceId, _ownerUserId);

        var exception = await act.Should().ThrowAsync<BusinessRuleViolationException>();
        exception.Which.Rule.Should().Be("LastOwner");
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenSpaceNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.RemoveMemberAsync(Guid.NewGuid(), _ownerUserId);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenMembershipNotFound_ReturnsFalse()
    {
        var result = await _sut.RemoveMemberAsync(_existingSpaceId, _memberUserId);

        result.Should().BeFalse();
    }

    // ── UpdateMemberRoleAsync ───────────────────────────────────────

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenValid_ReturnsUpdatedMembership()
    {
        // Add a second member to update
        await _sut.AddMemberAsync(_existingSpaceId, _memberUserId, SpaceRole.Viewer);

        var result = await _sut.UpdateMemberRoleAsync(
            _existingSpaceId, _memberUserId, SpaceRole.Editor);

        result.Role.Should().Be(SpaceRole.Editor);
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenLastOwnerDemoted_ThrowsBusinessRuleViolationException()
    {
        var act = () => _sut.UpdateMemberRoleAsync(
            _existingSpaceId, _ownerUserId, SpaceRole.Editor);

        var exception = await act.Should().ThrowAsync<BusinessRuleViolationException>();
        exception.Which.Rule.Should().Be("LastOwner");
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenSpaceNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.UpdateMemberRoleAsync(
            Guid.NewGuid(), _ownerUserId, SpaceRole.Editor);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenMembershipNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.UpdateMemberRoleAsync(
            _existingSpaceId, _memberUserId, SpaceRole.Editor);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
