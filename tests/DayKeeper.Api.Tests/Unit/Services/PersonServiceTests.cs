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

public sealed class PersonServiceTests : IDisposable
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _spaceId = Guid.NewGuid();
    private static readonly Guid _secondSpaceId = Guid.NewGuid();
    private static readonly Guid _personId = Guid.NewGuid();
    private static readonly DateTime _fixedTime =
        new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DayKeeperDbContext _context;
    private readonly PersonService _sut;

    public PersonServiceTests()
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

        var personRepository = new Repository<Person>(_context, dateTimeProvider);
        var contactMethodRepository = new Repository<ContactMethod>(_context, dateTimeProvider);
        var addressRepository = new Repository<Address>(_context, dateTimeProvider);
        var importantDateRepository = new Repository<ImportantDate>(_context, dateTimeProvider);
        var spaceRepository = new Repository<Space>(_context, dateTimeProvider);

        SeedData();

        _sut = new PersonService(
            personRepository, contactMethodRepository, addressRepository,
            importantDateRepository, spaceRepository, _context);
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
        _context.Set<Space>().Add(new Space
        {
            Id = _secondSpaceId,
            TenantId = _tenantId,
            Name = "Second Space",
            NormalizedName = "second space",
            SpaceType = SpaceType.Shared,
        });
        _context.Set<Person>().Add(new Person
        {
            Id = _personId,
            SpaceId = _spaceId,
            FirstName = "Existing",
            LastName = "Person",
            NormalizedFullName = "existing person",
        });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    // ── CreatePersonAsync ─────────────────────────────────────────

    [Fact]
    public async Task CreatePersonAsync_WhenValid_ReturnsPerson()
    {
        var result = await _sut.CreatePersonAsync(_spaceId, "John", "Doe", "Some notes");

        result.Should().NotBeNull();
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Notes.Should().Be("Some notes");
        result.SpaceId.Should().Be(_spaceId);
    }

    [Fact]
    public async Task CreatePersonAsync_WhenValid_SetsNormalizedFullName()
    {
        var result = await _sut.CreatePersonAsync(_spaceId, "John", "Doe", null);

        result.NormalizedFullName.Should().Be("john doe");
    }

    [Fact]
    public async Task CreatePersonAsync_WhenValid_TrimsFirstAndLastName()
    {
        var result = await _sut.CreatePersonAsync(_spaceId, "  John  ", "  Doe  ", null);

        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task CreatePersonAsync_SetsClientGeneratedId()
    {
        var result = await _sut.CreatePersonAsync(_spaceId, "John", "Doe", null);

        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreatePersonAsync_WhenSpaceNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CreatePersonAsync(Guid.NewGuid(), "John", "Doe", null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CreatePersonAsync_WhenDuplicateName_ThrowsDuplicatePersonNameException()
    {
        var act = () => _sut.CreatePersonAsync(_spaceId, "Existing", "Person", null);

        await act.Should().ThrowAsync<DuplicatePersonNameException>();
    }

    [Fact]
    public async Task CreatePersonAsync_WhenDuplicateNameDifferentCase_ThrowsDuplicatePersonNameException()
    {
        var act = () => _sut.CreatePersonAsync(_spaceId, "EXISTING", "PERSON", null);

        await act.Should().ThrowAsync<DuplicatePersonNameException>();
    }

    [Fact]
    public async Task CreatePersonAsync_WhenSameNameDifferentSpace_Succeeds()
    {
        var result = await _sut.CreatePersonAsync(_secondSpaceId, "Existing", "Person", null);

        result.Should().NotBeNull();
        result.SpaceId.Should().Be(_secondSpaceId);
    }

    // ── GetPersonAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetPersonAsync_WhenExists_ReturnsPerson()
    {
        var result = await _sut.GetPersonAsync(_personId);

        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Existing");
    }

    [Fact]
    public async Task GetPersonAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _sut.GetPersonAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── UpdatePersonAsync ─────────────────────────────────────────

    [Fact]
    public async Task UpdatePersonAsync_WhenFirstNameProvided_UpdatesFirstNameAndNormalized()
    {
        var result = await _sut.UpdatePersonAsync(_personId, "Updated", null, null);

        result.FirstName.Should().Be("Updated");
        result.NormalizedFullName.Should().Be("updated person");
    }

    [Fact]
    public async Task UpdatePersonAsync_WhenLastNameProvided_UpdatesLastNameAndNormalized()
    {
        var result = await _sut.UpdatePersonAsync(_personId, null, "Smith", null);

        result.LastName.Should().Be("Smith");
        result.NormalizedFullName.Should().Be("existing smith");
    }

    [Fact]
    public async Task UpdatePersonAsync_WhenNotesProvided_UpdatesNotes()
    {
        var result = await _sut.UpdatePersonAsync(_personId, null, null, "New notes");

        result.Notes.Should().Be("New notes");
    }

    [Fact]
    public async Task UpdatePersonAsync_WhenEmptyStringNotes_ClearsNotes()
    {
        // First set notes
        await _sut.UpdatePersonAsync(_personId, null, null, "Some notes");
        _context.ChangeTracker.Clear();

        // Then clear with empty string
        var result = await _sut.UpdatePersonAsync(_personId, null, null, "");

        result.Notes.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePersonAsync_WhenNotesNull_DoesNotChangeNotes()
    {
        await _sut.UpdatePersonAsync(_personId, null, null, "Original notes");
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdatePersonAsync(_personId, null, null, null);

        result.Notes.Should().Be("Original notes");
    }

    [Fact]
    public async Task UpdatePersonAsync_WhenAllFieldsNull_DoesNotChangeAnything()
    {
        var result = await _sut.UpdatePersonAsync(_personId, null, null, null);

        result.FirstName.Should().Be("Existing");
        result.LastName.Should().Be("Person");
        result.NormalizedFullName.Should().Be("existing person");
    }

    [Fact]
    public async Task UpdatePersonAsync_WhenNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.UpdatePersonAsync(Guid.NewGuid(), "Nope", null, null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UpdatePersonAsync_WhenDuplicateName_ThrowsDuplicatePersonNameException()
    {
        var second = await _sut.CreatePersonAsync(_spaceId, "Another", "Person", null);
        _context.ChangeTracker.Clear();

        var act = () => _sut.UpdatePersonAsync(second.Id, "Existing", "Person", null);

        await act.Should().ThrowAsync<DuplicatePersonNameException>();
    }

    [Fact]
    public async Task UpdatePersonAsync_WhenSameNameProvided_DoesNotThrow()
    {
        var act = () => _sut.UpdatePersonAsync(_personId, "Existing", "Person", null);

        await act.Should().NotThrowAsync();
    }

    // ── DeletePersonAsync ─────────────────────────────────────────

    [Fact]
    public async Task DeletePersonAsync_WhenExists_ReturnsTrue()
    {
        var created = await _sut.CreatePersonAsync(_spaceId, "ToDelete", "Person", null);
        _context.ChangeTracker.Clear();

        var result = await _sut.DeletePersonAsync(created.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePersonAsync_WhenExists_SoftDeletesEntity()
    {
        var created = await _sut.CreatePersonAsync(_spaceId, "ToDelete", "Person", null);
        _context.ChangeTracker.Clear();

        await _sut.DeletePersonAsync(created.Id);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetPersonAsync(created.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeletePersonAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _sut.DeletePersonAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── CreateContactMethodAsync ──────────────────────────────────

    [Fact]
    public async Task CreateContactMethodAsync_WhenValid_ReturnsContactMethod()
    {
        var result = await _sut.CreateContactMethodAsync(
            _personId, ContactMethodType.Phone, "555-1234", "Home", false);

        result.Should().NotBeNull();
        result.PersonId.Should().Be(_personId);
        result.Type.Should().Be(ContactMethodType.Phone);
        result.Value.Should().Be("555-1234");
        result.Label.Should().Be("Home");
    }

    [Fact]
    public async Task CreateContactMethodAsync_WithIsPrimaryTrue_SetsIsPrimary()
    {
        var result = await _sut.CreateContactMethodAsync(
            _personId, ContactMethodType.Email, "test@example.com", null, true);

        result.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task CreateContactMethodAsync_WithIsPrimaryFalse_SetsIsPrimaryFalse()
    {
        var result = await _sut.CreateContactMethodAsync(
            _personId, ContactMethodType.Email, "test@example.com", null, false);

        result.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task CreateContactMethodAsync_WhenPersonNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CreateContactMethodAsync(
            Guid.NewGuid(), ContactMethodType.Phone, "555-1234", null, false);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    // ── UpdateContactMethodAsync ──────────────────────────────────

    [Fact]
    public async Task UpdateContactMethodAsync_WhenTypeProvided_UpdatesType()
    {
        var cm = await _sut.CreateContactMethodAsync(
            _personId, ContactMethodType.Phone, "555-1234", null, false);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateContactMethodAsync(
            cm.Id, ContactMethodType.Email, null, null, null);

        result.Type.Should().Be(ContactMethodType.Email);
    }

    [Fact]
    public async Task UpdateContactMethodAsync_WhenValueProvided_UpdatesValueTrimmed()
    {
        var cm = await _sut.CreateContactMethodAsync(
            _personId, ContactMethodType.Phone, "555-1234", null, false);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateContactMethodAsync(
            cm.Id, null, "  555-5678  ", null, null);

        result.Value.Should().Be("555-5678");
    }

    [Fact]
    public async Task UpdateContactMethodAsync_WhenIsPrimaryProvided_UpdatesIsPrimary()
    {
        var cm = await _sut.CreateContactMethodAsync(
            _personId, ContactMethodType.Phone, "555-1234", null, false);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateContactMethodAsync(
            cm.Id, null, null, null, true);

        result.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateContactMethodAsync_WhenAllFieldsNull_DoesNotChangeAnything()
    {
        var cm = await _sut.CreateContactMethodAsync(
            _personId, ContactMethodType.Phone, "555-1234", "Home", false);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateContactMethodAsync(
            cm.Id, null, null, null, null);

        result.Type.Should().Be(ContactMethodType.Phone);
        result.Value.Should().Be("555-1234");
        result.Label.Should().Be("Home");
        result.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateContactMethodAsync_WhenNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.UpdateContactMethodAsync(
            Guid.NewGuid(), ContactMethodType.Phone, null, null, null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    // ── DeleteContactMethodAsync ──────────────────────────────────

    [Fact]
    public async Task DeleteContactMethodAsync_WhenExists_ReturnsTrue()
    {
        var cm = await _sut.CreateContactMethodAsync(
            _personId, ContactMethodType.Phone, "555-1234", null, false);
        _context.ChangeTracker.Clear();

        var result = await _sut.DeleteContactMethodAsync(cm.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteContactMethodAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _sut.DeleteContactMethodAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── CreateAddressAsync ────────────────────────────────────────

    [Fact]
    public async Task CreateAddressAsync_WhenValid_ReturnsAddress()
    {
        var result = await _sut.CreateAddressAsync(
            _personId, "Home", "123 Main St", "Apt 4", "Springfield", "IL", "62704", "US", false);

        result.Should().NotBeNull();
        result.PersonId.Should().Be(_personId);
        result.Label.Should().Be("Home");
        result.Street1.Should().Be("123 Main St");
        result.Street2.Should().Be("Apt 4");
        result.City.Should().Be("Springfield");
        result.State.Should().Be("IL");
        result.PostalCode.Should().Be("62704");
        result.Country.Should().Be("US");
    }

    [Fact]
    public async Task CreateAddressAsync_WithIsPrimaryTrue_SetsIsPrimary()
    {
        var result = await _sut.CreateAddressAsync(
            _personId, null, "123 Main St", null, "Springfield", null, null, "US", true);

        result.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAddressAsync_WhenPersonNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CreateAddressAsync(
            Guid.NewGuid(), null, "123 Main St", null, "Springfield", null, null, "US", false);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    // ── UpdateAddressAsync ────────────────────────────────────────

    [Fact]
    public async Task UpdateAddressAsync_WhenFieldsProvided_UpdatesFields()
    {
        var addr = await _sut.CreateAddressAsync(
            _personId, "Home", "123 Main St", null, "Springfield", null, null, "US", false);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateAddressAsync(
            addr.Id, "Work", "456 Oak Ave", "Suite 200", "Chicago", "IL", "60601", "US", true);

        result.Label.Should().Be("Work");
        result.Street1.Should().Be("456 Oak Ave");
        result.Street2.Should().Be("Suite 200");
        result.City.Should().Be("Chicago");
        result.State.Should().Be("IL");
        result.PostalCode.Should().Be("60601");
        result.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAddressAsync_WhenAllFieldsNull_DoesNotChangeAnything()
    {
        var addr = await _sut.CreateAddressAsync(
            _personId, "Home", "123 Main St", null, "Springfield", null, null, "US", false);
        _context.ChangeTracker.Clear();

        var result = await _sut.UpdateAddressAsync(
            addr.Id, null, null, null, null, null, null, null, null);

        result.Label.Should().Be("Home");
        result.Street1.Should().Be("123 Main St");
        result.City.Should().Be("Springfield");
        result.Country.Should().Be("US");
        result.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAddressAsync_WhenNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.UpdateAddressAsync(
            Guid.NewGuid(), "Nope", null, null, null, null, null, null, null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    // ── DeleteAddressAsync ────────────────────────────────────────

    [Fact]
    public async Task DeleteAddressAsync_WhenExists_ReturnsTrue()
    {
        var addr = await _sut.CreateAddressAsync(
            _personId, null, "123 Main St", null, "Springfield", null, null, "US", false);
        _context.ChangeTracker.Clear();

        var result = await _sut.DeleteAddressAsync(addr.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAddressAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _sut.DeleteAddressAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── CreateImportantDateAsync ──────────────────────────────────

    [Fact]
    public async Task CreateImportantDateAsync_WhenValid_ReturnsImportantDate()
    {
        var date = new DateOnly(1990, 6, 15);
        var result = await _sut.CreateImportantDateAsync(_personId, "Birthday", date, null);

        result.Should().NotBeNull();
        result.PersonId.Should().Be(_personId);
        result.Label.Should().Be("Birthday");
        result.Date.Should().Be(date);
        result.EventTypeId.Should().BeNull();
    }

    [Fact]
    public async Task CreateImportantDateAsync_WithNullEventTypeId_SetsEventTypeIdToNull()
    {
        var result = await _sut.CreateImportantDateAsync(
            _personId, "Anniversary", new DateOnly(2020, 8, 1), null);

        result.EventTypeId.Should().BeNull();
    }

    [Fact]
    public async Task CreateImportantDateAsync_WhenPersonNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.CreateImportantDateAsync(
            Guid.NewGuid(), "Birthday", new DateOnly(1990, 1, 1), null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    // ── UpdateImportantDateAsync ──────────────────────────────────

    [Fact]
    public async Task UpdateImportantDateAsync_WhenLabelProvided_UpdatesLabelTrimmed()
    {
        var id = await CreateImportantDateForTests();

        var result = await _sut.UpdateImportantDateAsync(id, "  Anniversary  ", null, null);

        result.Label.Should().Be("Anniversary");
    }

    [Fact]
    public async Task UpdateImportantDateAsync_WhenDateProvided_UpdatesDate()
    {
        var id = await CreateImportantDateForTests();
        var newDate = new DateOnly(2000, 12, 25);

        var result = await _sut.UpdateImportantDateAsync(id, null, newDate, null);

        result.Date.Should().Be(newDate);
    }

    [Fact]
    public async Task UpdateImportantDateAsync_WhenAllFieldsNull_DoesNotChangeAnything()
    {
        var id = await CreateImportantDateForTests();

        var result = await _sut.UpdateImportantDateAsync(id, null, null, null);

        result.Label.Should().Be("Birthday");
        result.Date.Should().Be(new DateOnly(1990, 6, 15));
    }

    [Fact]
    public async Task UpdateImportantDateAsync_WhenNotFound_ThrowsEntityNotFoundException()
    {
        var act = () => _sut.UpdateImportantDateAsync(Guid.NewGuid(), "Nope", null, null);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    // ── DeleteImportantDateAsync ──────────────────────────────────

    [Fact]
    public async Task DeleteImportantDateAsync_WhenExists_ReturnsTrue()
    {
        var id = await CreateImportantDateForTests();

        var result = await _sut.DeleteImportantDateAsync(id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteImportantDateAsync_WhenNotExists_ReturnsFalse()
    {
        var result = await _sut.DeleteImportantDateAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    // ── Helpers ───────────────────────────────────────────────────

    private async Task<Guid> CreateImportantDateForTests()
    {
        var importantDate = await _sut.CreateImportantDateAsync(
            _personId, "Birthday", new DateOnly(1990, 6, 15), null)
            .ConfigureAwait(false);
        _context.ChangeTracker.Clear();
        return importantDate.Id;
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
