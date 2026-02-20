namespace DayKeeper.Application.Interfaces;

/// <summary>
/// Abstraction over system clock for testability.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
