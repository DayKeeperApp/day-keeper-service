namespace DayKeeper.Application.Validation.Commands;

/// <summary>Validation command for updating an existing tenant.</summary>
public sealed record UpdateTenantCommand(Guid Id, string? Name, string? Slug);
