using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using DayKeeper.Infrastructure.Persistence.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Api.Tests.Unit.Configurations;

public sealed class AttachmentConfigurationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AttachmentTestDbContext _context;

    public AttachmentConfigurationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AttachmentTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AttachmentTestDbContext(options);
        _context.Database.EnsureCreated();
    }

    private static Tenant CreateTenant() => new() { Name = "Acme", Slug = "acme" };

    private static Space CreateSpace(Guid tenantId) => new()
    {
        TenantId = tenantId,
        Name = "Space",
        NormalizedName = "space",
        SpaceType = SpaceType.Personal,
    };

    private static Calendar CreateCalendar(Guid spaceId) => new()
    {
        SpaceId = spaceId,
        Name = "Calendar",
        NormalizedName = "calendar",
        Color = "#4A90D9",
        IsDefault = true,
    };

    private static CalendarEvent CreateEvent(Guid calendarId) => new()
    {
        CalendarId = calendarId,
        Title = "Event",
        Timezone = "UTC",
        StartAt = DateTime.UtcNow,
        EndAt = DateTime.UtcNow.AddHours(1),
    };

    private static TaskItem CreateTask(Guid spaceId) => new()
    {
        SpaceId = spaceId,
        Title = "Task",
        Status = TaskItemStatus.Open,
        Priority = TaskItemPriority.Medium,
    };

    private static Person CreatePerson(Guid spaceId) => new()
    {
        SpaceId = spaceId,
        FirstName = "Jane",
        LastName = "Doe",
        NormalizedFullName = "jane doe",
    };

    private static Attachment CreateAttachment(Guid tenantId, Guid? calendarEventId = null,
        Guid? taskItemId = null, Guid? personId = null) => new()
        {
            TenantId = tenantId,
            CalendarEventId = calendarEventId,
            TaskItemId = taskItemId,
            PersonId = personId,
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            FileSize = 1024,
            StoragePath = $"{tenantId}/test/{Guid.NewGuid()}/test.jpg",
        };

    // ── Cascade Delete ──────────────────────────────────────────────

    [Fact]
    public async Task CalendarEvent_CascadeDelete_RemovesAttachments()
    {
        var tenant = CreateTenant();
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var space = CreateSpace(tenant.Id);
        _context.Spaces.Add(space);
        await _context.SaveChangesAsync();

        var calendar = CreateCalendar(space.Id);
        _context.Calendars.Add(calendar);
        await _context.SaveChangesAsync();

        var evt = CreateEvent(calendar.Id);
        _context.CalendarEvents.Add(evt);
        await _context.SaveChangesAsync();

        _context.Attachments.Add(CreateAttachment(tenant.Id, calendarEventId: evt.Id));
        _context.Attachments.Add(CreateAttachment(tenant.Id, calendarEventId: evt.Id));
        await _context.SaveChangesAsync();

        _context.CalendarEvents.Remove(evt);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var remaining = await _context.Attachments.ToListAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task TaskItem_CascadeDelete_RemovesAttachments()
    {
        var tenant = CreateTenant();
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var space = CreateSpace(tenant.Id);
        _context.Spaces.Add(space);
        await _context.SaveChangesAsync();

        var task = CreateTask(space.Id);
        _context.TaskItems.Add(task);
        await _context.SaveChangesAsync();

        _context.Attachments.Add(CreateAttachment(tenant.Id, taskItemId: task.Id));
        await _context.SaveChangesAsync();

        _context.TaskItems.Remove(task);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var remaining = await _context.Attachments.ToListAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task Person_CascadeDelete_RemovesAttachments()
    {
        var tenant = CreateTenant();
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var space = CreateSpace(tenant.Id);
        _context.Spaces.Add(space);
        await _context.SaveChangesAsync();

        var person = CreatePerson(space.Id);
        _context.People.Add(person);
        await _context.SaveChangesAsync();

        _context.Attachments.Add(CreateAttachment(tenant.Id, personId: person.Id));
        await _context.SaveChangesAsync();

        _context.People.Remove(person);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var remaining = await _context.Attachments.ToListAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task Tenant_CascadeDelete_RemovesAttachments()
    {
        var tenant = CreateTenant();
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var space = CreateSpace(tenant.Id);
        _context.Spaces.Add(space);
        await _context.SaveChangesAsync();

        var person = CreatePerson(space.Id);
        _context.People.Add(person);
        await _context.SaveChangesAsync();

        _context.Attachments.Add(CreateAttachment(tenant.Id, personId: person.Id));
        await _context.SaveChangesAsync();

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var remaining = await _context.Attachments.ToListAsync();
        remaining.Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private sealed class AttachmentTestDbContext(
        DbContextOptions<AttachmentTestDbContext> options) : DbContext(options)
    {
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Space> Spaces => Set<Space>();
        public DbSet<Calendar> Calendars => Set<Calendar>();
        public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
        public DbSet<TaskItem> TaskItems => Set<TaskItem>();
        public DbSet<Person> People => Set<Person>();
        public DbSet<Attachment> Attachments => Set<Attachment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new TenantConfiguration().Configure(modelBuilder.Entity<Tenant>());
            new SpaceConfiguration().Configure(modelBuilder.Entity<Space>());
            new CalendarConfiguration().Configure(modelBuilder.Entity<Calendar>());
            new CalendarEventConfiguration().Configure(modelBuilder.Entity<CalendarEvent>());
            new TaskItemConfiguration().Configure(modelBuilder.Entity<TaskItem>());
            new PersonConfiguration().Configure(modelBuilder.Entity<Person>());
            new AttachmentConfiguration().Configure(modelBuilder.Entity<Attachment>());
        }
    }
}
