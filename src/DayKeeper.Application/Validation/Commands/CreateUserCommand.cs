using DayKeeper.Domain.Enums;

namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for creating a new user.</summary>
public sealed record CreateUserCommand(
    Guid TenantId,
    string DisplayName,
    string Email,
    string Timezone,
    WeekStart WeekStart,
    string? Locale);
