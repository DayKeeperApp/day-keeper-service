using Microsoft.Extensions.DependencyInjection;

namespace DayKeeper.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers application-layer services.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Future: register MediatR, AutoMapper, validators, etc.
        return services;
    }
}
