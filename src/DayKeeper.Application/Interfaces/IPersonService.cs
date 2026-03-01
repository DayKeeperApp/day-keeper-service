using DayKeeper.Application.Exceptions;
using DayKeeper.Domain.Entities;
using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Application service for managing persons and their associated
/// contact methods, addresses, and important dates.
/// Orchestrates business rules, validation, and persistence for
/// the People aggregate rooted at <see cref="Person"/>.
/// </summary>
public interface IPersonService
{
    // ── Person CRUD ─────────────────────────────────────────────

    /// <summary>
    /// Creates a new person within the specified space.
    /// </summary>
    /// <param name="spaceId">The space under which to create the person.</param>
    /// <param name="firstName">The person's first name.</param>
    /// <param name="lastName">The person's last name.</param>
    /// <param name="notes">Optional free-text notes.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The newly created person.</returns>
    /// <exception cref="EntityNotFoundException">The specified space does not exist.</exception>
    /// <exception cref="DuplicatePersonNameException">A person with the same normalized name already exists in this space.</exception>
    Task<Person> CreatePersonAsync(
        Guid spaceId,
        string firstName,
        string lastName,
        string? notes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a person by its unique identifier.
    /// </summary>
    /// <param name="personId">The unique identifier of the person.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The person if found; otherwise, <c>null</c>.</returns>
    Task<Person?> GetPersonAsync(
        Guid personId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the properties of an existing person.
    /// Pass <c>null</c> to leave a field unchanged. Pass an empty string for
    /// <paramref name="notes"/> to clear the notes field.
    /// </summary>
    /// <param name="personId">The unique identifier of the person to update.</param>
    /// <param name="firstName">The new first name, or <c>null</c> to leave unchanged.</param>
    /// <param name="lastName">The new last name, or <c>null</c> to leave unchanged.</param>
    /// <param name="notes">The new notes (<c>null</c> = leave unchanged, empty string = clear).</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The updated person.</returns>
    /// <exception cref="EntityNotFoundException">The person does not exist.</exception>
    /// <exception cref="DuplicatePersonNameException">The new name conflicts with an existing person in the same space.</exception>
    Task<Person> UpdatePersonAsync(
        Guid personId,
        string? firstName,
        string? lastName,
        string? notes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a person and all associated child entities.
    /// </summary>
    /// <param name="personId">The unique identifier of the person to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if the person was found and deleted; <c>false</c> if not found.</returns>
    Task<bool> DeletePersonAsync(
        Guid personId,
        CancellationToken cancellationToken = default);

    // ── ContactMethod CRUD ──────────────────────────────────────

    /// <summary>
    /// Creates a new contact method for the specified person.
    /// </summary>
    /// <param name="personId">The person to add the contact method to.</param>
    /// <param name="type">The type of contact method.</param>
    /// <param name="value">The contact value (e.g. phone number or email).</param>
    /// <param name="label">Optional label (e.g. "Work", "Home").</param>
    /// <param name="isPrimary">Whether this is the primary contact method of its type.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The newly created contact method.</returns>
    /// <exception cref="EntityNotFoundException">The specified person does not exist.</exception>
    Task<ContactMethod> CreateContactMethodAsync(
        Guid personId,
        ContactMethodType type,
        string value,
        string? label,
        bool isPrimary,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the properties of an existing contact method.
    /// </summary>
    /// <param name="id">The unique identifier of the contact method to update.</param>
    /// <param name="type">The new type, or <c>null</c> to leave unchanged.</param>
    /// <param name="value">The new value, or <c>null</c> to leave unchanged.</param>
    /// <param name="label">The new label, or <c>null</c> to leave unchanged.</param>
    /// <param name="isPrimary">The new primary flag, or <c>null</c> to leave unchanged.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The updated contact method.</returns>
    /// <exception cref="EntityNotFoundException">The contact method does not exist.</exception>
    Task<ContactMethod> UpdateContactMethodAsync(
        Guid id,
        ContactMethodType? type,
        string? value,
        string? label,
        bool? isPrimary,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a contact method.
    /// </summary>
    /// <param name="id">The unique identifier of the contact method to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if found and deleted; <c>false</c> if not found.</returns>
    Task<bool> DeleteContactMethodAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // ── Address CRUD ────────────────────────────────────────────

    /// <summary>
    /// Creates a new address for the specified person.
    /// </summary>
    /// <param name="personId">The person to add the address to.</param>
    /// <param name="label">Optional label (e.g. "Home", "Work").</param>
    /// <param name="street1">The primary street address.</param>
    /// <param name="street2">Optional secondary street address.</param>
    /// <param name="city">The city name.</param>
    /// <param name="state">Optional state or province.</param>
    /// <param name="postalCode">Optional postal or zip code.</param>
    /// <param name="country">The country name.</param>
    /// <param name="isPrimary">Whether this is the primary address.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The newly created address.</returns>
    /// <exception cref="EntityNotFoundException">The specified person does not exist.</exception>
    Task<Address> CreateAddressAsync(
        Guid personId,
        string? label,
        string street1,
        string? street2,
        string city,
        string? state,
        string? postalCode,
        string country,
        bool isPrimary,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the properties of an existing address.
    /// </summary>
    /// <param name="id">The unique identifier of the address to update.</param>
    /// <param name="label">The new label, or <c>null</c> to leave unchanged.</param>
    /// <param name="street1">The new street1, or <c>null</c> to leave unchanged.</param>
    /// <param name="street2">The new street2, or <c>null</c> to leave unchanged.</param>
    /// <param name="city">The new city, or <c>null</c> to leave unchanged.</param>
    /// <param name="state">The new state, or <c>null</c> to leave unchanged.</param>
    /// <param name="postalCode">The new postal code, or <c>null</c> to leave unchanged.</param>
    /// <param name="country">The new country, or <c>null</c> to leave unchanged.</param>
    /// <param name="isPrimary">The new primary flag, or <c>null</c> to leave unchanged.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The updated address.</returns>
    /// <exception cref="EntityNotFoundException">The address does not exist.</exception>
    Task<Address> UpdateAddressAsync(
        Guid id,
        string? label,
        string? street1,
        string? street2,
        string? city,
        string? state,
        string? postalCode,
        string? country,
        bool? isPrimary,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes an address.
    /// </summary>
    /// <param name="id">The unique identifier of the address to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if found and deleted; <c>false</c> if not found.</returns>
    Task<bool> DeleteAddressAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // ── ImportantDate CRUD ──────────────────────────────────────

    /// <summary>
    /// Creates a new important date for the specified person.
    /// </summary>
    /// <param name="personId">The person to add the important date to.</param>
    /// <param name="label">The label for this date (e.g. "Birthday").</param>
    /// <param name="dateValue">The calendar date.</param>
    /// <param name="eventTypeId">Optional event type association.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The newly created important date.</returns>
    /// <exception cref="EntityNotFoundException">The specified person does not exist.</exception>
    Task<ImportantDate> CreateImportantDateAsync(
        Guid personId,
        string label,
        DateOnly dateValue,
        Guid? eventTypeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the properties of an existing important date.
    /// </summary>
    /// <param name="id">The unique identifier of the important date to update.</param>
    /// <param name="label">The new label, or <c>null</c> to leave unchanged.</param>
    /// <param name="dateValue">The new date, or <c>null</c> to leave unchanged.</param>
    /// <param name="eventTypeId">The new event type ID, or <c>null</c> to leave unchanged.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The updated important date.</returns>
    /// <exception cref="EntityNotFoundException">The important date does not exist.</exception>
    Task<ImportantDate> UpdateImportantDateAsync(
        Guid id,
        string? label,
        DateOnly? dateValue,
        Guid? eventTypeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes an important date.
    /// </summary>
    /// <param name="id">The unique identifier of the important date to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns><c>true</c> if found and deleted; <c>false</c> if not found.</returns>
    Task<bool> DeleteImportantDateAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
