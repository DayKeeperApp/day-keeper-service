using DayKeeper.Api.Tests.Helpers;
using DayKeeper.Application.Exceptions;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Persistence;
using DayKeeper.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Services;

public sealed class SpaceAuthorizationServiceTests : IDisposable
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _spaceId = Guid.NewGuid();
    private static readonly Guid _viewerUserId = Guid.NewGuid();
    private static readonly Guid _editorUserId = Guid.NewGuid();
    private static readonly Guid _ownerUserId = Guid.NewGuid();
    private static readonly Guid _nonMemberUserId = Guid.NewGuid();

    private readonly SqliteConnection _connection;
    private readonly DayKeeperDbContext _context;
    private readonly SpaceAuthorizationService _sut;

    public SpaceAuthorizationServiceTests()
    {
        var tenantContext = new TestTenantContext { CurrentTenantId = _tenantId };

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DayKeeperDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new DayKeeperDbContext(options, tenantContext);
        _context.Database.EnsureCreated();

        SeedData();

        _sut = new SpaceAuthorizationService(_context);
    }

    private void SeedData()
    {
        var tenant = new Tenant { Id = _tenantId, Name = "Test Tenant", Slug = "test-tenant" };
        _context.Set<Tenant>().Add(tenant);

        var space = new Space
        {
            Id = _spaceId,
            TenantId = _tenantId,
            Name = "Test Space",
            NormalizedName = "test space",
            SpaceType = SpaceType.Shared,
        };
        _context.Set<Space>().Add(space);

        _context.Set<User>().AddRange(
            new User { Id = _viewerUserId, TenantId = _tenantId, DisplayName = "Viewer", Email = "viewer@test.com", Timezone = "UTC", WeekStart = WeekStart.Monday },
            new User { Id = _editorUserId, TenantId = _tenantId, DisplayName = "Editor", Email = "editor@test.com", Timezone = "UTC", WeekStart = WeekStart.Monday },
            new User { Id = _ownerUserId, TenantId = _tenantId, DisplayName = "Owner", Email = "owner@test.com", Timezone = "UTC", WeekStart = WeekStart.Monday },
            new User { Id = _nonMemberUserId, TenantId = _tenantId, DisplayName = "NonMember", Email = "nonmember@test.com", Timezone = "UTC", WeekStart = WeekStart.Monday });

        _context.Set<SpaceMembership>().AddRange(
            new SpaceMembership { SpaceId = _spaceId, UserId = _viewerUserId, Role = SpaceRole.Viewer },
            new SpaceMembership { SpaceId = _spaceId, UserId = _editorUserId, Role = SpaceRole.Editor },
            new SpaceMembership { SpaceId = _spaceId, UserId = _ownerUserId, Role = SpaceRole.Owner });

        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    // ── GetUserRoleAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetUserRoleAsync_WhenUserIsViewer_ReturnsViewer()
    {
        var result = await _sut.GetUserRoleAsync(_spaceId, _viewerUserId);

        result.Should().Be(SpaceRole.Viewer);
    }

    [Fact]
    public async Task GetUserRoleAsync_WhenUserIsEditor_ReturnsEditor()
    {
        var result = await _sut.GetUserRoleAsync(_spaceId, _editorUserId);

        result.Should().Be(SpaceRole.Editor);
    }

    [Fact]
    public async Task GetUserRoleAsync_WhenUserIsOwner_ReturnsOwner()
    {
        var result = await _sut.GetUserRoleAsync(_spaceId, _ownerUserId);

        result.Should().Be(SpaceRole.Owner);
    }

    [Fact]
    public async Task GetUserRoleAsync_WhenUserIsNotMember_ReturnsNull()
    {
        var result = await _sut.GetUserRoleAsync(_spaceId, _nonMemberUserId);

        result.Should().BeNull();
    }

    // ── HasMinimumRoleAsync ───────────────────────────────────────────

    [Fact]
    public async Task HasMinimumRoleAsync_WhenViewerCheckedForViewer_ReturnsTrue()
    {
        var result = await _sut.HasMinimumRoleAsync(_spaceId, _viewerUserId, SpaceRole.Viewer);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasMinimumRoleAsync_WhenViewerCheckedForEditor_ReturnsFalse()
    {
        var result = await _sut.HasMinimumRoleAsync(_spaceId, _viewerUserId, SpaceRole.Editor);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasMinimumRoleAsync_WhenEditorCheckedForEditor_ReturnsTrue()
    {
        var result = await _sut.HasMinimumRoleAsync(_spaceId, _editorUserId, SpaceRole.Editor);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasMinimumRoleAsync_WhenEditorCheckedForOwner_ReturnsFalse()
    {
        var result = await _sut.HasMinimumRoleAsync(_spaceId, _editorUserId, SpaceRole.Owner);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasMinimumRoleAsync_WhenOwnerCheckedForViewer_ReturnsTrue()
    {
        var result = await _sut.HasMinimumRoleAsync(_spaceId, _ownerUserId, SpaceRole.Viewer);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasMinimumRoleAsync_WhenOwnerCheckedForOwner_ReturnsTrue()
    {
        var result = await _sut.HasMinimumRoleAsync(_spaceId, _ownerUserId, SpaceRole.Owner);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasMinimumRoleAsync_WhenNonMember_ReturnsFalse()
    {
        var result = await _sut.HasMinimumRoleAsync(_spaceId, _nonMemberUserId, SpaceRole.Viewer);

        result.Should().BeFalse();
    }

    // ── CanViewAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CanViewAsync_WhenUserIsMember_ReturnsTrue()
    {
        var result = await _sut.CanViewAsync(_spaceId, _viewerUserId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanViewAsync_WhenUserIsNotMember_ReturnsFalse()
    {
        var result = await _sut.CanViewAsync(_spaceId, _nonMemberUserId);

        result.Should().BeFalse();
    }

    // ── CanEditAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CanEditAsync_WhenUserIsEditor_ReturnsTrue()
    {
        var result = await _sut.CanEditAsync(_spaceId, _editorUserId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanEditAsync_WhenUserIsOwner_ReturnsTrue()
    {
        var result = await _sut.CanEditAsync(_spaceId, _ownerUserId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanEditAsync_WhenUserIsViewer_ReturnsFalse()
    {
        var result = await _sut.CanEditAsync(_spaceId, _viewerUserId);

        result.Should().BeFalse();
    }

    // ── IsOwnerAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task IsOwnerAsync_WhenUserIsOwner_ReturnsTrue()
    {
        var result = await _sut.IsOwnerAsync(_spaceId, _ownerUserId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsOwnerAsync_WhenUserIsEditor_ReturnsFalse()
    {
        var result = await _sut.IsOwnerAsync(_spaceId, _editorUserId);

        result.Should().BeFalse();
    }

    // ── EnsureMinimumRoleAsync ────────────────────────────────────────

    [Fact]
    public async Task EnsureMinimumRoleAsync_WhenUserHasRequiredRole_DoesNotThrow()
    {
        var act = () => _sut.EnsureMinimumRoleAsync(_spaceId, _ownerUserId, SpaceRole.Editor);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureMinimumRoleAsync_WhenUserLacksRequiredRole_ThrowsBusinessRuleViolationException()
    {
        var act = () => _sut.EnsureMinimumRoleAsync(_spaceId, _viewerUserId, SpaceRole.Editor);

        var exception = await act.Should().ThrowAsync<BusinessRuleViolationException>();
        exception.Which.Rule.Should().Be("InsufficientSpaceRole");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
