using DayKeeper.Api.Tests.Helpers;
using DayKeeper.Application.Interfaces;
using DayKeeper.Infrastructure;
using DayKeeper.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace DayKeeper.Api.Tests.Unit.Services;

public sealed class QuartzSchedulerRegistrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceCollection _services;
    private readonly ServiceProvider _serviceProvider;

    public QuartzSchedulerRegistrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test",
            })
            .Build();

        _services = new ServiceCollection();
        _services.AddLogging();
        _services.AddSingleton<ITenantContext>(new TestTenantContext { CurrentTenantId = null });
        _services.AddInfrastructureServices(configuration);

        // Override the Npgsql DbContext with SQLite for testing
        _services.AddDbContext<DayKeeperDbContext>((sp, options) =>
            options.UseSqlite(_connection));

        _serviceProvider = _services.BuildServiceProvider();
    }

    [Fact]
    public void AddInfrastructureServices_RegistersSchedulerFactory()
    {
        var factory = _serviceProvider.GetService<ISchedulerFactory>();

        factory.Should().NotBeNull();
    }

    [Fact]
    public async Task SchedulerFactory_CreatesWorkingScheduler()
    {
        var factory = _serviceProvider.GetRequiredService<ISchedulerFactory>();

        var scheduler = await factory.GetScheduler();

        scheduler.Should().NotBeNull();
        scheduler.SchedulerName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AddInfrastructureServices_RegistersQuartzHostedService()
    {
        _services.Should().Contain(sd =>
            sd.ServiceType == typeof(IHostedService)
            && sd.ImplementationType != null
            && sd.ImplementationType.Name.Contains("Quartz", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        _connection.Dispose();
    }
}
