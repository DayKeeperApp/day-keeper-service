using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for creating a new contact method.</summary>
public sealed record CreateContactMethodCommand(
    Guid PersonId,
    ContactMethodType Type,
    string Value,
    string? Label,
    bool IsPrimary);
