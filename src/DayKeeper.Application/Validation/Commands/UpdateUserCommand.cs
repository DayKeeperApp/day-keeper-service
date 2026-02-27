using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for updating an existing user.</summary>
public sealed record UpdateUserCommand(
    Guid Id,
    string? DisplayName,
    string? Email,
    string? Timezone,
    WeekStart? WeekStart,
    string? Locale);
