using DayKeeper.Application.Interfaces;

namespace DayKeeper.Infrastructure.Services;

/// <summary>
/// Production implementation of <see cref="IDateTimeProvider"/> that returns the real system clock.
/// </summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
