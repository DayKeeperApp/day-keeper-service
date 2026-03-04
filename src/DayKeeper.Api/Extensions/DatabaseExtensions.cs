using DayKeeper.Infrastructure.Persistence;

namespace DayKeeper.Api.Extensions;

/// <summary>
/// Extension methods for database initialization at application startup.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Seeds the database with system reference data and optional development fixtures.
    /// Safe to call on every startup (idempotent).
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DayKeeperDbContext>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(typeof(DbInitializer));

        await DbInitializer.SeedAsync(
            context,
            app.Environment.IsDevelopment(),
            logger).ConfigureAwait(false);
    }
}
