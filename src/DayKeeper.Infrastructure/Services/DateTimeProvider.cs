using DayKeeper.Application.Interfaces;

namespace DayKeeper.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
