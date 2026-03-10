using DayKeeper.Application.Interfaces;
using DayKeeper.Infrastructure.Persistence;
using DayKeeper.Infrastructure.Persistence.Interceptors;
using DayKeeper.Infrastructure.Persistence.Repositories;
using DayKeeper.Infrastructure.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

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
        services.AddSingleton<ISyncSerializer, SyncSerializer>();
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
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ISyncService, SyncService>();
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IShoppingListService, ShoppingListService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IDeviceNotificationPreferenceService, DeviceNotificationPreferenceService>();
        services.AddScoped<IReminderSchedulerService, ReminderSchedulerService>();
        services.AddSingleton<INotificationSender, FcmNotificationSender>();

        AddScheduler(services);
        AddFirebase(services, configuration);

        services.AddHealthChecks()
            .AddDbContextCheck<DayKeeperDbContext>();

        return services;
    }

    /// <summary>
    /// Configures Quartz.NET scheduler with in-memory job store and hosted service.
    /// Jobs are resolved via Microsoft DI with scoped lifetime by default.
    /// </summary>
    private static void AddScheduler(IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            q.UseInMemoryStore();
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });
    }

    /// <summary>
    /// Initializes the Firebase Admin SDK using application-default credentials
    /// (via the <c>GOOGLE_APPLICATION_CREDENTIALS</c> environment variable).
    /// Logs a warning and continues if credentials are not configured.
    /// </summary>
    private static void AddFirebase(IServiceCollection services, IConfiguration configuration)
    {
        if (FirebaseApp.DefaultInstance is not null)
        {
            return;
        }

        try
        {
            var options = new AppOptions
            {
                Credential = GoogleCredential.GetApplicationDefault(),
            };

            var projectId = configuration["Firebase:ProjectId"];
            if (!string.IsNullOrEmpty(projectId))
            {
                options.ProjectId = projectId;
            }

            FirebaseApp.Create(options);
        }
        catch (Exception) when (FirebaseApp.DefaultInstance is null)
        {
            // Firebase credentials not configured (GOOGLE_APPLICATION_CREDENTIALS not set).
            // Push notifications will be unavailable. This is expected in local dev and tests.
            // At runtime, FcmNotificationSender will throw when FirebaseMessaging.DefaultInstance is null.
        }
    }
}
