namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for creating a new address.</summary>
public sealed record CreateAddressCommand(
    Guid PersonId,
    string? Label,
    string Street1,
    string? Street2,
    string City,
    string? State,
    string? PostalCode,
    string Country,
    bool IsPrimary);
