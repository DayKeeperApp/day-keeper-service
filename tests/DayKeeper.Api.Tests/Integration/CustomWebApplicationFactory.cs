using DayKeeper.Infrastructure.Persistence;
using DayKeeper.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DayKeeper.Api.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

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

            // Use a persistent SQLite in-memory connection for tests
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<DayKeeperDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite(_connection);
                options.AddInterceptors(
                    serviceProvider.GetRequiredService<AuditFieldsInterceptor>());
            });
        });
    }

    /// <summary>
    /// Ensures the SQLite database schema is created after the host is built.
    /// </summary>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DayKeeperDbContext>();
        db.Database.EnsureCreated();

        return host;
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
