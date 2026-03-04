using DayKeeper.Application.Interfaces;
using DayKeeper.Infrastructure.Persistence;
using DayKeeper.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace DayKeeper.Api.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Open the SQLite connection early and create the schema so that
        // DbInitializer.SeedAsync (called from Program.cs on startup) finds tables.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        EnsureSchemaCreated(_connection);

        builder.ConfigureTestServices(services =>
        {
            // Remove all EF Core DbContext registrations including provider configuration
            // services registered by AddDbContext: DbContextOptions<T>, DbContextOptions,
            // DayKeeperDbContext, and IDbContextOptionsConfiguration<T> (which holds the
            // UseNpgsql action). All must be removed to avoid dual-provider conflicts.
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DayKeeperDbContext) ||
                    d.ServiceType == typeof(DbContextOptions<DayKeeperDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    (d.ServiceType.IsGenericType &&
                        d.ServiceType.GetGenericTypeDefinition().FullName?.Contains("IDbContextOptionsConfiguration") == true))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Use the persistent SQLite in-memory connection for tests
            services.AddDbContext<DayKeeperDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite(_connection);
                options.AddInterceptors(
                    serviceProvider.GetRequiredService<AuditFieldsInterceptor>(),
                    serviceProvider.GetRequiredService<ChangeLogInterceptor>());
            });
        });
    }

    /// <summary>
    /// Creates the database schema on the SQLite connection using a temporary context.
    /// This must happen before the host starts because Program.cs runs DbInitializer on startup.
    /// </summary>
    private static void EnsureSchemaCreated(SqliteConnection connection)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DayKeeperDbContext>();
        optionsBuilder.UseSqlite(connection);

        var tenantContext = Substitute.For<ITenantContext>();
        using var context = new DayKeeperDbContext(optionsBuilder.Options, tenantContext);
        context.Database.EnsureCreated();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
        }

        base.Dispose(disposing);
    }
}
