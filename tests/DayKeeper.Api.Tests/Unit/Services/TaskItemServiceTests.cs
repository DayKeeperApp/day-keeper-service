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

public sealed class TaskItemServiceTests : IDisposable
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _secondTenantId = Guid.NewGuid();
    private static readonly Guid _spaceId = Guid.NewGuid();
    private static readonly Guid _secondSpaceId = Guid.NewGuid();
    private static readonly Guid _projectId = Guid.NewGuid();
    private static readonly Guid _secondSpaceProjectId = Guid.NewGuid();
    private static readonly Guid _tenantCategoryId = Guid.NewGuid();
    private static readonly Guid _systemCategoryId = Guid.NewGuid();
    private static readonly Guid _otherTenantCategoryId = Guid.NewGuid();
    private static readonly DateTime _fixedTime =
        new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DayKeeperDbContext _context;
    private readonly TaskItemService _sut;
    private readonly IRecurrenceExpander _recurrenceExpander;

    public TaskItemServiceTests()
    {
        var tenantContext = new TestTenantContext { CurrentTenantId = _tenantId };
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(_fixedTime);

        _recurrenceExpander = Substitute.For<IRecurrenceExpander>();

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DayKeeperDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new DayKeeperDbContext(options, tenantContext);
        _context.Database.EnsureCreated();

        var taskItemRepository = new Repository<TaskItem>(_context, dateTimeProvider);
        var spaceRepository = new Repository<Space>(_context, dateTimeProvider);
        var projectRepository = new Repository<Project>(_context, dateTimeProvider);
        var categoryRepository = new Repository<Category>(_context, dateTimeProvider);
        var taskCategoryRepository = new Repository<TaskCategory>(_context, dateTimeProvider);

        SeedData();

        _sut = new TaskItemService(
            taskItemRepository, spaceRepository, projectRepository,
            categoryRepository, taskCategoryRepository,
            dateTimeProvider, _recurrenceExpander, _context);
    }

    private void SeedData()
    {
        SeedTenants();
        SeedSpaces();
        SeedProjects();
        SeedCategories();
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    private void SeedTenants()
    {
        _context.Set<Tenant>().AddRange(
            new Tenant { Id = _tenantId, Name = "Test Tenant", Slug = "test-tenant" },
            new Tenant { Id = _secondTenantId, Name = "Other Tenant", Slug = "other-tenant" });
    }

    private void SeedSpaces()
    {
        _context.Set<Space>().AddRange(
            new Space { Id = _spaceId, TenantId = _tenantId, Name = "Test Space", NormalizedName = "test space", SpaceType = SpaceType.Personal },
            new Space { Id = _secondSpaceId, TenantId = _tenantId, Name = "Second Space", NormalizedName = "second space", SpaceType = SpaceType.Shared });
    }

    private void SeedProjects()
    {
        _context.Set<Project>().AddRange(
            new Project { Id = _projectId, SpaceId = _spaceId, Name = "Test Project", NormalizedName = "test project" },
            new Project { Id = _secondSpaceProjectId, SpaceId = _secondSpaceId, Name = "Other Project", NormalizedName = "other project" });
    }

    private void SeedCategories()
    {
        _context.Set<Category>().AddRange(
            new Category { Id = _tenantCategoryId, TenantId = _tenantId, Name = "Work", NormalizedName = "work", Color = "#FF0000" },
            new Category { Id = _systemCategoryId, TenantId = null, Name = "General", NormalizedName = "general", Color = "#00FF00" },
            new Category { Id = _otherTenantCategoryId, TenantId = _secondTenantId, Name = "Private", NormalizedName = "private", Color = "#0000FF" });
    }

    // ── CreateTaskItemAsync ──────────────────────────────────────────

    [Fact]
    public async Task CreateTaskItemAsync_WhenValid_ReturnsTaskItem()
    {
        var result = await _sut.CreateTaskItemAsync(
            _spaceId, "Buy groceries", null, null,
            TaskItemStatus.Open, TaskItemPriority.Medium, null, null, null);

        result.Should().NotBeNull();
        result.Title.Should().Be("Buy groceries");
        result.SpaceId.Should().Be(_spaceId);
        result.Status.Should().Be(TaskItemStatus.Open);
        result.Priority.Should().Be(TaskItemPriority.Medium);
    }

    [Fact]
    public async Task CreateTaskItemAsync_WithAllOptionalFields_SetsFieldsCorrectly()
    {
        var dueAt = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);
        var dueDate = new DateOnly(2026, 4, 1);

        var result = await _sut.CreateTaskItemAsync(
            _spaceId, "Recurring task", "A description", _projectId,
            TaskItemStatus.InProgress, TaskItemPriority.High,
            dueAt, dueDate, "FREQ=DAILY;COUNT=5");

        result.Description.Should().Be("A description");
        result.ProjectId.Should().Be(_projectId);
        result.DueAt.Should().Be(dueAt);
        result.DueDate.Should().Be(dueDate);
        result.RecurrenceRule.Should().Be("FREQ=DAILY;COUNT=5");
    }

    [Fact]
    public async Task CreateTaskItemAsync_WithValidProject_SetsProjectId()
    {
        var result = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, _projectId,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);

        result.ProjectId.Should().Be(_projectId);
    }

    [Fact]
    public async Task CreateTaskItemAsync_WithProjectInWrongSpace_ThrowsBusinessRuleViolationException()
    {
        var act = () => _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, _secondSpaceProjectId,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public async Task CreateTaskItemAsync_WhenSpaceNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CreateTaskItemAsync(
            Guid.NewGuid(), "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CreateTaskItemAsync_WhenProjectNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, Guid.NewGuid(),
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CreateTaskItemAsync_SetsClientGeneratedId()
    {
        var result = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);

        result.Id.Should().NotBe(Guid.Empty);
    }

    // ── GetTaskItemAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetTaskItemAsync_WhenExists_ReturnsTaskItem()
    {
        var created = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetTaskItemAsync(created.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Task");
    }

    [Fact]
    public async Task GetTaskItemAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _sut.GetTaskItemAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── UpdateTaskItemAsync ──────────────────────────────────────────

    [Fact]
    public async Task UpdateTaskItemAsync_TitleOnly_PreservesOtherFields()
    {
        var created = await _sut.CreateTaskItemAsync(
            _spaceId, "Original", "desc", _projectId,
            TaskItemStatus.Open, TaskItemPriority.High, null, null, null);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateTaskItemAsync(
            created.Id, "Updated", null, null, null, null, null, null, null);

        result.Title.Should().Be("Updated");
        result.Description.Should().Be("desc");
        result.ProjectId.Should().Be(_projectId);
        result.Priority.Should().Be(TaskItemPriority.High);
    }

    [Fact]
    public async Task UpdateTaskItemAsync_StatusToCompleted_SetsCompletedAt()
    {
        var created = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateTaskItemAsync(
            created.Id, null, null, TaskItemStatus.Completed, null, null, null, null, null);

        result.Status.Should().Be(TaskItemStatus.Completed);
        result.CompletedAt.Should().Be(_fixedTime);
    }

    [Fact]
    public async Task UpdateTaskItemAsync_StatusFromCompletedToOpen_ClearsCompletedAt()
    {
        var created = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        await _sut.CompleteTaskItemAsync(created.Id);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateTaskItemAsync(
            created.Id, null, null, TaskItemStatus.Open, null, null, null, null, null);

        result.Status.Should().Be(TaskItemStatus.Open);
        result.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskItemAsync_StatusUnchanged_DoesNotAlterCompletedAt()
    {
        var created = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateTaskItemAsync(
            created.Id, "Renamed", null, null, null, null, null, null, null);

        result.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskItemAsync_ProjectIdEmpty_UnassignsProject()
    {
        var created = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, _projectId,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateTaskItemAsync(
            created.Id, null, null, null, null, Guid.Empty, null, null, null);

        result.ProjectId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskItemAsync_ProjectIdToValid_UpdatesProject()
    {
        var created = await _sut.CreateTaskItemAsync(
            _secondSpaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateTaskItemAsync(
            created.Id, null, null, null, null, _secondSpaceProjectId, null, null, null);

        result.ProjectId.Should().Be(_secondSpaceProjectId);
    }

    [Fact]
    public async Task UpdateTaskItemAsync_ProjectIdInWrongSpace_ThrowsBusinessRuleViolationException()
    {
        var created = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        _context.ChangeTracker.Clear();

        var act = () => _sut.UpdateTaskItemAsync(
            created.Id, null, null, null, null, _secondSpaceProjectId, null, null, null);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public async Task UpdateTaskItemAsync_WhenNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.UpdateTaskItemAsync(
            Guid.NewGuid(), "Nope", null, null, null, null, null, null, null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    // ── CompleteTaskItemAsync ─────────────────────────────────────────

    [Fact]
    public async Task CompleteTaskItemAsync_SetsStatusAndCompletedAt()
    {
        var created = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        _context.ChangeTracker.Clear();

        var result = await _sut.CompleteTaskItemAsync(created.Id);

        result.Status.Should().Be(TaskItemStatus.Completed);
        result.CompletedAt.Should().Be(_fixedTime);
    }

    [Fact]
    public async Task CompleteTaskItemAsync_WhenNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CompleteTaskItemAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    // ── DeleteTaskItemAsync ──────────────────────────────────────────

    [Fact]
    public async Task DeleteTaskItemAsync_WhenExists_ReturnsTrue()
    {
        var created = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        _context.ChangeTracker.Clear();

        var result = await _sut.DeleteTaskItemAsync(created.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTaskItemAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _sut.DeleteTaskItemAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTaskItemAsync_WhenExists_SoftDeletesEntity()
    {
        var created = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        _context.ChangeTracker.Clear();

        await _sut.DeleteTaskItemAsync(created.Id);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetTaskItemAsync(created.Id);
        result.Should().BeNull();
    }

    // ── AssignCategoryAsync ──────────────────────────────────────────

    [Fact]
    public async Task AssignCategoryAsync_TenantScopedCategory_ReturnsTaskCategory()
    {
        var task = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        _context.ChangeTracker.Clear();

        var result = await _sut.AssignCategoryAsync(task.Id, _tenantCategoryId);

        result.Should().NotBeNull();
        result.TaskItemId.Should().Be(task.Id);
        result.CategoryId.Should().Be(_tenantCategoryId);
    }

    [Fact]
    public async Task AssignCategoryAsync_SystemCategory_AllowedForAnySpace()
    {
        var task = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        _context.ChangeTracker.Clear();

        var result = await _sut.AssignCategoryAsync(task.Id, _systemCategoryId);

        result.Should().NotBeNull();
        result.CategoryId.Should().Be(_systemCategoryId);
    }

    [Fact]
    public async Task AssignCategoryAsync_DuplicateAssignment_ThrowsBusinessRuleViolationException()
    {
        var task = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        await _sut.AssignCategoryAsync(task.Id, _tenantCategoryId);
        _context.ChangeTracker.Clear();

        var act = () => _sut.AssignCategoryAsync(task.Id, _tenantCategoryId);

        var ex = await act.Should().ThrowAsync<BusinessRuleViolationException>();
        ex.Which.Rule.Should().Be("DuplicateCategoryAssignment");
    }

    [Fact]
    public async Task AssignCategoryAsync_CategoryFromDifferentTenant_ThrowsEntityNotFoundException()
    {
        // The tenant query filter prevents cross-tenant category lookups,
        // so the category is effectively "not found" for the current tenant.
        var task = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        _context.ChangeTracker.Clear();

        var act = () => _sut.AssignCategoryAsync(task.Id, _otherTenantCategoryId);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task AssignCategoryAsync_TaskNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.AssignCategoryAsync(Guid.NewGuid(), _tenantCategoryId);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task AssignCategoryAsync_CategoryNotFound_ThrowsEntityNotFoundException()
    {
        var task = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        _context.ChangeTracker.Clear();

        var act = () => _sut.AssignCategoryAsync(task.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    // ── RemoveCategoryAsync ──────────────────────────────────────────

    [Fact]
    public async Task RemoveCategoryAsync_WhenAssigned_ReturnsTrue()
    {
        var task = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        await _sut.AssignCategoryAsync(task.Id, _tenantCategoryId);
        _context.ChangeTracker.Clear();

        var result = await _sut.RemoveCategoryAsync(task.Id, _tenantCategoryId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveCategoryAsync_WhenNotAssigned_ReturnsFalse()
    {
        var task = await _sut.CreateTaskItemAsync(
            _spaceId, "Task", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        _context.ChangeTracker.Clear();

        var result = await _sut.RemoveCategoryAsync(task.Id, _tenantCategoryId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveCategoryAsync_TaskNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.RemoveCategoryAsync(Guid.NewGuid(), _tenantCategoryId);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    // ── GetRecurringOccurrencesAsync ─────────────────────────────────

    [Fact]
    public async Task GetRecurringOccurrencesAsync_WithDueAt_ReturnsOccurrences()
    {
        var dueAt = new DateTime(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);
        var rangeStart = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc);
        var expected = new List<DateTime>
        {
            new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc),
            new(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc),
        };

        _recurrenceExpander.GetOccurrences(
            "FREQ=DAILY;COUNT=5", dueAt, "America/New_York", rangeStart, rangeEnd)
            .Returns(expected);

        var task = await _sut.CreateTaskItemAsync(
            _spaceId, "Recurring", null, null,
            TaskItemStatus.Open, TaskItemPriority.None,
            dueAt, null, "FREQ=DAILY;COUNT=5");
        _context.ChangeTracker.Clear();

        var result = await _sut.GetRecurringOccurrencesAsync(
            task.Id, "America/New_York", rangeStart, rangeEnd);

        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetRecurringOccurrencesAsync_WithDueDateFallback_UsesDateAsMidnightUtc()
    {
        var dueDate = new DateOnly(2026, 3, 1);
        var expectedSeriesStart = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeStart = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc);

        _recurrenceExpander.GetOccurrences(
            "FREQ=DAILY", expectedSeriesStart, "UTC", rangeStart, rangeEnd)
            .Returns(new List<DateTime> { expectedSeriesStart });

        var task = await _sut.CreateTaskItemAsync(
            _spaceId, "Date only", null, null,
            TaskItemStatus.Open, TaskItemPriority.None,
            null, dueDate, "FREQ=DAILY");
        _context.ChangeTracker.Clear();

        var result = await _sut.GetRecurringOccurrencesAsync(
            task.Id, "UTC", rangeStart, rangeEnd);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRecurringOccurrencesAsync_NoRecurrenceRule_ThrowsBusinessRuleViolationException()
    {
        var task = await _sut.CreateTaskItemAsync(
            _spaceId, "No recurrence", null, null,
            TaskItemStatus.Open, TaskItemPriority.None, null, null, null);
        _context.ChangeTracker.Clear();

        var act = () => _sut.GetRecurringOccurrencesAsync(
            task.Id, "UTC",
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc));

        var ex = await act.Should().ThrowAsync<BusinessRuleViolationException>();
        ex.Which.Rule.Should().Be("NoRecurrenceRule");
    }

    [Fact]
    public async Task GetRecurringOccurrencesAsync_NoDueAtOrDueDate_ThrowsBusinessRuleViolationException()
    {
        var task = await _sut.CreateTaskItemAsync(
            _spaceId, "No dates", null, null,
            TaskItemStatus.Open, TaskItemPriority.None,
            null, null, "FREQ=DAILY");
        _context.ChangeTracker.Clear();

        var act = () => _sut.GetRecurringOccurrencesAsync(
            task.Id, "UTC",
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc));

        var ex = await act.Should().ThrowAsync<BusinessRuleViolationException>();
        ex.Which.Rule.Should().Be("NoSeriesStart");
    }

    [Fact]
    public async Task GetRecurringOccurrencesAsync_TaskNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.GetRecurringOccurrencesAsync(
            Guid.NewGuid(), "UTC",
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc));

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
