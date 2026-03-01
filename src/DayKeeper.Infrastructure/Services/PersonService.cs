using DayKeeper.Application.Exceptions;
using DayKeeper.Application.Interfaces;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of <see cref="IPersonService"/>.
/// Orchestrates business rules for person, contact method, address,
/// and important date management using repository abstractions and
/// direct DbContext queries.
/// </summary>
public sealed class PersonService(
    IRepository<Person> personRepository,
    IRepository<ContactMethod> contactMethodRepository,
    IRepository<Address> addressRepository,
    IRepository<ImportantDate> importantDateRepository,
    IRepository<Space> spaceRepository,
    DbContext dbContext) : IPersonService
{
    private readonly IRepository<Person> _personRepository = personRepository;
    private readonly IRepository<ContactMethod> _contactMethodRepository = contactMethodRepository;
    private readonly IRepository<Address> _addressRepository = addressRepository;
    private readonly IRepository<ImportantDate> _importantDateRepository = importantDateRepository;
    private readonly IRepository<Space> _spaceRepository = spaceRepository;
    private readonly DbContext _dbContext = dbContext;

    // ── Person CRUD ─────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<Person> CreatePersonAsync(
        Guid spaceId,
        string firstName,
        string lastName,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        _ = await _spaceRepository.GetByIdAsync(spaceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Space), spaceId);

        var normalizedFullName = BuildNormalizedFullName(firstName, lastName);

        var nameExists = await _dbContext.Set<Person>()
            .AnyAsync(p => p.SpaceId == spaceId && p.NormalizedFullName == normalizedFullName,
                cancellationToken)
            .ConfigureAwait(false);

        if (nameExists)
        {
            throw new DuplicatePersonNameException(spaceId, normalizedFullName);
        }

        var person = new Person
        {
            SpaceId = spaceId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            NormalizedFullName = normalizedFullName,
            Notes = notes,
        };

        return await _personRepository.AddAsync(person, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Person?> GetPersonAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        return await _personRepository.GetByIdAsync(personId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Person> UpdatePersonAsync(
        Guid personId,
        string? firstName,
        string? lastName,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var person = await _personRepository.GetByIdAsync(personId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Person), personId);

        var nameChanged = false;
        var newFirstName = person.FirstName;
        var newLastName = person.LastName;

        if (firstName is not null)
        {
            newFirstName = firstName.Trim();
            nameChanged = true;
        }

        if (lastName is not null)
        {
            newLastName = lastName.Trim();
            nameChanged = true;
        }

        if (nameChanged)
        {
            var normalizedFullName = BuildNormalizedFullName(newFirstName, newLastName);

            if (!string.Equals(normalizedFullName, person.NormalizedFullName, StringComparison.Ordinal))
            {
                var nameExists = await _dbContext.Set<Person>()
                    .AnyAsync(p => p.SpaceId == person.SpaceId
                                && p.NormalizedFullName == normalizedFullName
                                && p.Id != personId,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (nameExists)
                {
                    throw new DuplicatePersonNameException(person.SpaceId, normalizedFullName);
                }
            }

            person.FirstName = newFirstName;
            person.LastName = newLastName;
            person.NormalizedFullName = BuildNormalizedFullName(newFirstName, newLastName);
        }

        // Empty string clears notes; null leaves unchanged
        if (notes is not null)
        {
            person.Notes = notes.Length == 0 ? null : notes;
        }

        await _personRepository.UpdateAsync(person, cancellationToken)
            .ConfigureAwait(false);

        return person;
    }

    /// <inheritdoc />
    public async Task<bool> DeletePersonAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        return await _personRepository.DeleteAsync(personId, cancellationToken)
            .ConfigureAwait(false);
    }

    // ── ContactMethod CRUD ──────────────────────────────────────

    /// <inheritdoc />
    public async Task<ContactMethod> CreateContactMethodAsync(
        Guid personId,
        ContactMethodType type,
        string value,
        string? label,
        bool isPrimary,
        CancellationToken cancellationToken = default)
    {
        _ = await _personRepository.GetByIdAsync(personId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Person), personId);

        var contactMethod = new ContactMethod
        {
            PersonId = personId,
            Type = type,
            Value = value.Trim(),
            Label = label?.Trim(),
            IsPrimary = isPrimary,
        };

        return await _contactMethodRepository.AddAsync(contactMethod, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ContactMethod> UpdateContactMethodAsync(
        Guid id,
        ContactMethodType? type,
        string? value,
        string? label,
        bool? isPrimary,
        CancellationToken cancellationToken = default)
    {
        var contactMethod = await _contactMethodRepository.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(ContactMethod), id);

        if (type.HasValue)
        {
            contactMethod.Type = type.Value;
        }

        if (value is not null)
        {
            contactMethod.Value = value.Trim();
        }

        if (label is not null)
        {
            contactMethod.Label = label.Trim();
        }

        if (isPrimary.HasValue)
        {
            contactMethod.IsPrimary = isPrimary.Value;
        }

        await _contactMethodRepository.UpdateAsync(contactMethod, cancellationToken)
            .ConfigureAwait(false);

        return contactMethod;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteContactMethodAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _contactMethodRepository.DeleteAsync(id, cancellationToken)
            .ConfigureAwait(false);
    }

    // ── Address CRUD ────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<Address> CreateAddressAsync(
        Guid personId,
        string? label,
        string street1,
        string? street2,
        string city,
        string? state,
        string? postalCode,
        string country,
        bool isPrimary,
        CancellationToken cancellationToken = default)
    {
        _ = await _personRepository.GetByIdAsync(personId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Person), personId);

        var address = new Address
        {
            PersonId = personId,
            Label = label?.Trim(),
            Street1 = street1.Trim(),
            Street2 = street2?.Trim(),
            City = city.Trim(),
            State = state?.Trim(),
            PostalCode = postalCode?.Trim(),
            Country = country.Trim(),
            IsPrimary = isPrimary,
        };

        return await _addressRepository.AddAsync(address, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Address> UpdateAddressAsync(
        Guid id,
        string? label,
        string? street1,
        string? street2,
        string? city,
        string? state,
        string? postalCode,
        string? country,
        bool? isPrimary,
        CancellationToken cancellationToken = default)
    {
        var address = await _addressRepository.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Address), id);

        if (label is not null)
        {
            address.Label = label.Trim();
        }

        if (street1 is not null)
        {
            address.Street1 = street1.Trim();
        }

        if (street2 is not null)
        {
            address.Street2 = street2.Trim();
        }

        if (city is not null)
        {
            address.City = city.Trim();
        }

        if (state is not null)
        {
            address.State = state.Trim();
        }

        if (postalCode is not null)
        {
            address.PostalCode = postalCode.Trim();
        }

        if (country is not null)
        {
            address.Country = country.Trim();
        }

        if (isPrimary.HasValue)
        {
            address.IsPrimary = isPrimary.Value;
        }

        await _addressRepository.UpdateAsync(address, cancellationToken)
            .ConfigureAwait(false);

        return address;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAddressAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _addressRepository.DeleteAsync(id, cancellationToken)
            .ConfigureAwait(false);
    }

    // ── ImportantDate CRUD ──────────────────────────────────────

    /// <inheritdoc />
    public async Task<ImportantDate> CreateImportantDateAsync(
        Guid personId,
        string label,
        DateOnly dateValue,
        Guid? eventTypeId,
        CancellationToken cancellationToken = default)
    {
        _ = await _personRepository.GetByIdAsync(personId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(Person), personId);

        var importantDate = new ImportantDate
        {
            PersonId = personId,
            Label = label.Trim(),
            Date = dateValue,
            EventTypeId = eventTypeId,
        };

        return await _importantDateRepository.AddAsync(importantDate, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ImportantDate> UpdateImportantDateAsync(
        Guid id,
        string? label,
        DateOnly? dateValue,
        Guid? eventTypeId,
        CancellationToken cancellationToken = default)
    {
        var importantDate = await _importantDateRepository.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new EntityNotFoundException(nameof(ImportantDate), id);

        if (label is not null)
        {
            importantDate.Label = label.Trim();
        }

        if (dateValue.HasValue)
        {
            importantDate.Date = dateValue.Value;
        }

        if (eventTypeId.HasValue)
        {
            importantDate.EventTypeId = eventTypeId.Value;
        }

        await _importantDateRepository.UpdateAsync(importantDate, cancellationToken)
            .ConfigureAwait(false);

        return importantDate;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteImportantDateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _importantDateRepository.DeleteAsync(id, cancellationToken)
            .ConfigureAwait(false);
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static string BuildNormalizedFullName(string firstName, string lastName)
        => $"{firstName} {lastName}".Trim().ToLowerInvariant();
}
