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
using Microsoft.Extensions.Configuration;

namespace DayKeeper.Api.Tests.Unit.Services;

public sealed class AttachmentServiceTests : IDisposable
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _spaceId = Guid.NewGuid();
    private static readonly Guid _calendarId = Guid.NewGuid();
    private static readonly Guid _calendarEventId = Guid.NewGuid();
    private static readonly Guid _taskItemId = Guid.NewGuid();
    private static readonly Guid _personId = Guid.NewGuid();
    private static readonly DateTime _fixedTime =
        new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DayKeeperDbContext _context;
    private readonly IAttachmentStorageService _storageService;
    private readonly AttachmentService _sut;

    public AttachmentServiceTests()
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

        var attachmentRepository = new Repository<Attachment>(_context, dateTimeProvider);
        var calendarEventRepository = new Repository<CalendarEvent>(_context, dateTimeProvider);
        var taskItemRepository = new Repository<TaskItem>(_context, dateTimeProvider);
        var personRepository = new Repository<Person>(_context, dateTimeProvider);
        _storageService = Substitute.For<IAttachmentStorageService>();

        var config = BuildConfig();

        SeedData();

        _sut = new AttachmentService(
            attachmentRepository, calendarEventRepository, taskItemRepository,
            personRepository, _storageService, config);
    }

    private void SeedData()
    {
        _context.Set<Tenant>().Add(new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
        });
        _context.Set<Space>().Add(new Space
        {
            Id = _spaceId,
            TenantId = _tenantId,
            Name = "Test Space",
            NormalizedName = "test space",
            SpaceType = SpaceType.Personal,
        });
        _context.Set<Calendar>().Add(new Calendar
        {
            Id = _calendarId,
            SpaceId = _spaceId,
            Name = "Test Calendar",
            NormalizedName = "test calendar",
            Color = "#4A90D9",
            IsDefault = true,
        });
        _context.Set<CalendarEvent>().Add(new CalendarEvent
        {
            Id = _calendarEventId,
            CalendarId = _calendarId,
            Title = "Test Event",
            Timezone = "America/Chicago",
            StartAt = _fixedTime,
            EndAt = _fixedTime.AddHours(1),
        });
        _context.Set<TaskItem>().Add(new TaskItem
        {
            Id = _taskItemId,
            SpaceId = _spaceId,
            Title = "Test Task",
            Status = TaskItemStatus.Open,
            Priority = TaskItemPriority.Medium,
        });
        _context.Set<Person>().Add(new Person
        {
            Id = _personId,
            SpaceId = _spaceId,
            FirstName = "Jane",
            LastName = "Doe",
            NormalizedFullName = "jane doe",
        });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    private static IConfiguration BuildConfig(long maxFileSizeBytes = 10 * 1024 * 1024) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["AttachmentStorage:MaxFileSizeBytes"] = maxFileSizeBytes.ToString(System.Globalization.CultureInfo.InvariantCulture),
            })
            .Build();

    private static MemoryStream CreateStream(int sizeBytes = 128)
    {
        var data = new byte[sizeBytes];
        Array.Fill<byte>(data, 0x42);
        return new MemoryStream(data);
    }

    private Task<Attachment> CreateForCalendarEvent(
        string fileName = "photo.jpg",
        string contentType = "image/jpeg",
        int fileSize = 128) =>
        _sut.CreateAttachmentAsync(
            _tenantId, _calendarEventId, null, null,
            fileName, contentType, CreateStream(fileSize));

    // ── Content Type Validation ─────────────────────────────────────

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("image/heic")]
    [InlineData("application/pdf")]
    public async Task CreateAttachmentAsync_WithAllowedContentType_Succeeds(string contentType)
    {
        var result = await _sut.CreateAttachmentAsync(
            _tenantId, _calendarEventId, null, null,
            "file.bin", contentType, CreateStream());

        result.Should().NotBeNull();
        result.ContentType.Should().Be(contentType);
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/zip")]
    [InlineData("image/gif")]
    [InlineData("video/mp4")]
    public async Task CreateAttachmentAsync_WithDisallowedContentType_ThrowsInputValidationException(string contentType)
    {
        var act = () => _sut.CreateAttachmentAsync(
            _tenantId, _calendarEventId, null, null,
            "file.bin", contentType, CreateStream());

        var ex = await act.Should().ThrowAsync<InputValidationException>();
        ex.Which.Errors.Should().ContainKey("contentType");
    }

    [Fact]
    public async Task CreateAttachmentAsync_WithUpperCaseContentType_Succeeds()
    {
        var result = await _sut.CreateAttachmentAsync(
            _tenantId, _calendarEventId, null, null,
            "file.jpg", "IMAGE/JPEG", CreateStream());

        result.Should().NotBeNull();
    }

    // ── File Size Limit Enforcement ─────────────────────────────────

    [Fact]
    public async Task CreateAttachmentAsync_WithFileSizeAtLimit_Succeeds()
    {
        const int limit = 10 * 1024 * 1024;
        var result = await _sut.CreateAttachmentAsync(
            _tenantId, _calendarEventId, null, null,
            "big.jpg", "image/jpeg", CreateStream(limit));

        result.Should().NotBeNull();
        result.FileSize.Should().Be(limit);
    }

    [Fact]
    public async Task CreateAttachmentAsync_WithFileSizeExceedingLimit_ThrowsInputValidationException()
    {
        const int overLimit = 10 * 1024 * 1024 + 1;
        var act = () => _sut.CreateAttachmentAsync(
            _tenantId, _calendarEventId, null, null,
            "huge.jpg", "image/jpeg", CreateStream(overLimit));

        var ex = await act.Should().ThrowAsync<InputValidationException>();
        ex.Which.Errors.Should().ContainKey("file");
    }

    [Fact]
    public async Task CreateAttachmentAsync_WithEmptyFile_Succeeds()
    {
        var result = await _sut.CreateAttachmentAsync(
            _tenantId, _calendarEventId, null, null,
            "empty.jpg", "image/jpeg", CreateStream(0));

        result.Should().NotBeNull();
        result.FileSize.Should().Be(0);
    }

    [Fact]
    public async Task CreateAttachmentAsync_WithCustomMaxFileSize_EnforcesConfiguredLimit()
    {
        var tenantContext = new TestTenantContext { CurrentTenantId = _tenantId };
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(_fixedTime);

        var attachmentRepo = new Repository<Attachment>(_context, dateTimeProvider);
        var eventRepo = new Repository<CalendarEvent>(_context, dateTimeProvider);
        var taskRepo = new Repository<TaskItem>(_context, dateTimeProvider);
        var personRepo = new Repository<Person>(_context, dateTimeProvider);

        var customSut = new AttachmentService(
            attachmentRepo, eventRepo, taskRepo, personRepo,
            _storageService, BuildConfig(maxFileSizeBytes: 1024));

        var act = () => customSut.CreateAttachmentAsync(
            _tenantId, _calendarEventId, null, null,
            "big.jpg", "image/jpeg", CreateStream(1025));

        var ex = await act.Should().ThrowAsync<InputValidationException>();
        ex.Which.Errors.Should().ContainKey("file");
    }

    // ── Storage Path Generation ─────────────────────────────────────

    [Fact]
    public async Task CreateAttachmentAsync_ForCalendarEvent_GeneratesCorrectStoragePath()
    {
        var result = await CreateForCalendarEvent();

        result.StoragePath.Should().StartWith($"{_tenantId}/calendarEvent/{_calendarEventId}/");
        result.StoragePath.Should().EndWith("/photo.jpg");
        await _storageService.Received(1).SaveAsync(
            Arg.Is<string>(s => s == result.StoragePath),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAttachmentAsync_ForTaskItem_GeneratesCorrectStoragePath()
    {
        var result = await _sut.CreateAttachmentAsync(
            _tenantId, null, _taskItemId, null,
            "doc.pdf", "application/pdf", CreateStream());

        result.StoragePath.Should().StartWith($"{_tenantId}/taskItem/{_taskItemId}/");
        result.StoragePath.Should().EndWith("/doc.pdf");
    }

    [Fact]
    public async Task CreateAttachmentAsync_ForPerson_GeneratesCorrectStoragePath()
    {
        var result = await _sut.CreateAttachmentAsync(
            _tenantId, null, null, _personId,
            "avatar.png", "image/png", CreateStream());

        result.StoragePath.Should().StartWith($"{_tenantId}/person/{_personId}/");
        result.StoragePath.Should().EndWith("/avatar.png");
    }

    [Fact]
    public async Task CreateAttachmentAsync_WithPathInFileName_SanitizesPath()
    {
        var result = await _sut.CreateAttachmentAsync(
            _tenantId, _calendarEventId, null, null,
            "../../../etc/passwd", "application/pdf", CreateStream());

        result.FileName.Should().Be("passwd");
        result.StoragePath.Should().EndWith("/passwd");
        result.StoragePath.Should().NotContain("..");
    }

    // ── Single Parent Validation ────────────────────────────────────

    [Fact]
    public async Task CreateAttachmentAsync_WithNoParent_ThrowsInputValidationException()
    {
        var act = () => _sut.CreateAttachmentAsync(
            _tenantId, null, null, null,
            "file.jpg", "image/jpeg", CreateStream());

        var ex = await act.Should().ThrowAsync<InputValidationException>();
        ex.Which.Errors.Should().ContainKey("parentId");
    }

    [Fact]
    public async Task CreateAttachmentAsync_WithMultipleParents_ThrowsInputValidationException()
    {
        var act = () => _sut.CreateAttachmentAsync(
            _tenantId, _calendarEventId, _taskItemId, null,
            "file.jpg", "image/jpeg", CreateStream());

        var ex = await act.Should().ThrowAsync<InputValidationException>();
        ex.Which.Errors.Should().ContainKey("parentId");
    }

    [Fact]
    public async Task CreateAttachmentAsync_WithAllParents_ThrowsInputValidationException()
    {
        var act = () => _sut.CreateAttachmentAsync(
            _tenantId, _calendarEventId, _taskItemId, _personId,
            "file.jpg", "image/jpeg", CreateStream());

        var ex = await act.Should().ThrowAsync<InputValidationException>();
        ex.Which.Errors.Should().ContainKey("parentId");
    }

    // ── File Name Validation ────────────────────────────────────────

    [Fact]
    public async Task CreateAttachmentAsync_WithEmptyFileName_ThrowsInputValidationException()
    {
        var act = () => _sut.CreateAttachmentAsync(
            _tenantId, _calendarEventId, null, null,
            "", "image/jpeg", CreateStream());

        var ex = await act.Should().ThrowAsync<InputValidationException>();
        ex.Which.Errors.Should().ContainKey("fileName");
    }

    [Fact]
    public async Task CreateAttachmentAsync_WithWhitespaceFileName_ThrowsInputValidationException()
    {
        var act = () => _sut.CreateAttachmentAsync(
            _tenantId, _calendarEventId, null, null,
            "   ", "image/jpeg", CreateStream());

        var ex = await act.Should().ThrowAsync<InputValidationException>();
        ex.Which.Errors.Should().ContainKey("fileName");
    }

    [Fact]
    public async Task CreateAttachmentAsync_WithFileNameExceeding512Chars_ThrowsInputValidationException()
    {
        var longName = new string('a', 509) + ".jpg";
        var act = () => _sut.CreateAttachmentAsync(
            _tenantId, _calendarEventId, null, null,
            longName, "image/jpeg", CreateStream());

        var ex = await act.Should().ThrowAsync<InputValidationException>();
        ex.Which.Errors.Should().ContainKey("fileName");
    }

    // ── Parent Exists Validation ────────────────────────────────────

    [Fact]
    public async Task CreateAttachmentAsync_WithNonExistentCalendarEvent_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CreateAttachmentAsync(
            _tenantId, Guid.NewGuid(), null, null,
            "file.jpg", "image/jpeg", CreateStream());

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CreateAttachmentAsync_WithNonExistentTaskItem_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CreateAttachmentAsync(
            _tenantId, null, Guid.NewGuid(), null,
            "file.jpg", "image/jpeg", CreateStream());

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CreateAttachmentAsync_WithNonExistentPerson_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CreateAttachmentAsync(
            _tenantId, null, null, Guid.NewGuid(),
            "file.jpg", "image/jpeg", CreateStream());

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    // ── GetAttachmentStreamAsync ────────────────────────────────────

    [Fact]
    public async Task GetAttachmentStreamAsync_WhenExists_ReturnsAttachmentAndStream()
    {
        var created = await CreateForCalendarEvent();
        _context.ChangeTracker.Clear();

        var expectedStream = new MemoryStream([0x01, 0x02, 0x03]);
        _storageService.ReadAsync(created.StoragePath, Arg.Any<CancellationToken>())
            .Returns(expectedStream);

        var (attachment, stream) = await _sut.GetAttachmentStreamAsync(created.Id);

        attachment.Id.Should().Be(created.Id);
        attachment.FileName.Should().Be("photo.jpg");
        stream.Should().BeSameAs(expectedStream);
    }

    [Fact]
    public async Task GetAttachmentStreamAsync_WhenNotExists_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.GetAttachmentStreamAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    // ── DeleteAttachmentAsync ───────────────────────────────────────

    [Fact]
    public async Task DeleteAttachmentAsync_WhenExists_ReturnsTrueAndCallsStorageDelete()
    {
        var created = await CreateForCalendarEvent();
        _context.ChangeTracker.Clear();
        _storageService.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.DeleteAttachmentAsync(created.Id);

        result.Should().BeTrue();
        await _storageService.Received(1).DeleteAsync(
            created.StoragePath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAttachmentAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _sut.DeleteAttachmentAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── Error Handling ──────────────────────────────────────────────

    [Fact]
    public async Task CreateAttachmentAsync_WhenStorageFails_PropagatesException()
    {
        _storageService.SaveAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new IOException("Disk full")));

        var act = () => _sut.CreateAttachmentAsync(
            _tenantId, _calendarEventId, null, null,
            "file.jpg", "image/jpeg", CreateStream());

        await act.Should().ThrowAsync<IOException>().WithMessage("Disk full");
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
