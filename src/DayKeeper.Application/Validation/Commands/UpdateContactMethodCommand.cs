using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for updating an existing contact method.</summary>
public sealed record UpdateContactMethodCommand(
    Guid Id,
    ContactMethodType? Type,
    string? Value,
    string? Label,
    bool? IsPrimary);
