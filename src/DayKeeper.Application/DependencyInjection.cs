using DayKeeper.Application.Validation.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DayKeeper.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers application-layer services.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateTenantCommandValidator>();

        return services;
    }
}
