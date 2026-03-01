namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for updating an existing address.</summary>
public sealed record UpdateAddressCommand(
    Guid Id,
    string? Label,
    string? Street1,
    string? Street2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    bool? IsPrimary);
