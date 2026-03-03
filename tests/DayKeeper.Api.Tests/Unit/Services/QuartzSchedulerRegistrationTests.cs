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

public sealed class QuartzSchedulerRegistrationTests : IClassFixture<QuartzSchedulerRegistrationTests.Fixture>
{
    private readonly Fixture _fixture;

    public QuartzSchedulerRegistrationTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void AddInfrastructureServices_RegistersSchedulerFactory()
    {
        var factory = _fixture.ServiceProvider.GetService<ISchedulerFactory>();

        factory.Should().NotBeNull();
    }

    [Fact]
    public async Task SchedulerFactory_CreatesWorkingScheduler()
    {
        var factory = _fixture.ServiceProvider.GetRequiredService<ISchedulerFactory>();

        var scheduler = await factory.GetScheduler();

        scheduler.Should().NotBeNull();
        scheduler.SchedulerName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AddInfrastructureServices_RegistersQuartzHostedService()
    {
        _fixture.Services.Should().Contain(sd =>
            sd.ServiceType == typeof(IHostedService)
            && sd.ImplementationType != null
            && sd.ImplementationType.Name.Contains("Quartz", StringComparison.Ordinal));
    }

    /// <summary>
    /// Shared fixture so all tests use a single ServiceProvider / scheduler instance.
    /// The ServiceProvider is intentionally NOT disposed: Quartz's static LogProvider
    /// holds a global reference to the DI LoggerFactory, and disposing it would corrupt
    /// Quartz initialization in the integration tests that run in parallel.
    /// The scheduler is shut down to remove the entry from the global SchedulerRepository.
    /// </summary>
    public sealed class Fixture : IAsyncLifetime, IDisposable
    {
        private readonly SqliteConnection _connection;

        public ServiceCollection Services { get; }
        public ServiceProvider ServiceProvider { get; }

        public Fixture()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test",
                })
                .Build();

            Services = new ServiceCollection();
            Services.AddLogging();
            Services.AddSingleton<ITenantContext>(new TestTenantContext { CurrentTenantId = null });
            Services.AddInfrastructureServices(configuration);

            // Give the test scheduler a unique name so it doesn't collide with the
            // integration tests' Quartz scheduler in the global SchedulerRepository.
            Services.AddQuartz(q =>
                q.SchedulerName = $"UnitTestScheduler_{Guid.NewGuid():N}");

            // Override the Npgsql DbContext with SQLite for testing
            Services.AddDbContext<DayKeeperDbContext>((sp, options) =>
                options.UseSqlite(_connection));

            ServiceProvider = Services.BuildServiceProvider();
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            // Shut down the scheduler to remove it from the global SchedulerRepository.
            try
            {
                var factory = ServiceProvider.GetService<ISchedulerFactory>();
                if (factory is not null)
                {
                    var scheduler = await factory.GetScheduler().ConfigureAwait(false);
                    if (!scheduler.IsShutdown)
                    {
                        await scheduler.Shutdown(waitForJobsToComplete: false).ConfigureAwait(false);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Safe to ignore — scheduler may already be torn down.
            }
        }

        public void Dispose()
        {
            _connection.Dispose();
            // ServiceProvider is intentionally NOT disposed here — see class summary.
        }
    }
}
