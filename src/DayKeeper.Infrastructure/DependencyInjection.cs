using DayKeeper.Application.Interfaces;
using DayKeeper.Infrastructure.Persistence;
using DayKeeper.Infrastructure.Persistence.Interceptors;
using DayKeeper.Infrastructure.Persistence.Repositories;
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
        services.AddSingleton<IAttachmentStorageService, AttachmentStorageService>();
        services.AddSingleton<IRecurrenceExpander, IcalNetRecurrenceExpander>();
        services.AddSingleton<AuditFieldsInterceptor>();
        services.AddScoped<ChangeLogInterceptor>();

        services.AddDbContext<DayKeeperDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditFieldsInterceptor>(),
                serviceProvider.GetRequiredService<ChangeLogInterceptor>());
        });

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<DayKeeperDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ISpaceAuthorizationService, SpaceAuthorizationService>();
        services.AddScoped<ISpaceService, SpaceService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskItemService, TaskItemService>();
        services.AddScoped<ISyncService, SyncService>();

        services.AddHealthChecks()
            .AddDbContextCheck<DayKeeperDbContext>();

        return services;
    }
}
