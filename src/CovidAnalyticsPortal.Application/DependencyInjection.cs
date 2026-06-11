using System.Reflection;
using CovidAnalyticsPortal.Application.Common.Behaviours;
using CovidAnalyticsPortal.Application.Common.Interfaces;
using CovidAnalyticsPortal.Application.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CovidAnalyticsPortal.Application;

/// <summary>
/// Composition-root helpers for the application layer. Centralising the service
/// registrations here keeps the dependency wiring close to the components it
/// describes and gives the host a single, intention-revealing entry point.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the application layer's MediatR handlers, pipeline behaviours,
    /// FluentValidation validators, and application services.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(assembly));

        // Pipeline behaviours run in registration order: log first, then validate.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        return services;
    }
}
