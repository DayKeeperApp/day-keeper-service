using DayKeeper.Application.Interfaces;
using DayKeeper.Infrastructure.Persistence;
using DayKeeper.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DayKeeper.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure-layer services (implementations, data access, external services).
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddDbContext<DayKeeperDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddHealthChecks()
            .AddDbContextCheck<DayKeeperDbContext>();

        return services;
    }
}
