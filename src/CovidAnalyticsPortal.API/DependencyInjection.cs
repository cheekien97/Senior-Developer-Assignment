using Asp.Versioning;
using CovidAnalyticsPortal.API.Context;
using CovidAnalyticsPortal.API.Swagger;
using CovidAnalyticsPortal.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Threading.RateLimiting;

namespace CovidAnalyticsPortal.API;

/// <summary>
/// Composition-root helpers for the API (presentation) layer. Registers
/// controllers, API versioning, versioned Swagger, the HTTP request context
/// accessor, and rate limiting. Keeping this wiring in one place keeps
/// <c>Program.cs</c> readable.
/// </summary>
public static class DependencyInjection
{
    /// <summary>The name of the global fixed-window rate-limiting policy.</summary>
    public const string GlobalRateLimitPolicy = "global-fixed-window";

    /// <summary>
    /// Registers the API-layer services with the dependency-injection container.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentContext, HttpCurrentContext>();

        services.AddApiVersioningSupport();
        services.AddSwaggerSupport();
        services.AddRateLimitingSupport();

        return services;
    }

    private static IServiceCollection AddApiVersioningSupport(this IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }

    private static IServiceCollection AddSwaggerSupport(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.ConfigureOptions<ConfigureSwaggerOptions>();
        services.AddSwaggerGen(options =>
        {
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    private static IServiceCollection AddRateLimitingSupport(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Per-client fixed-window limiter partitioned by remote IP address
            // (falling back to a shared partition when the address is unknown).
            // Provides baseline anti-automation/abuse protection for the public,
            // unauthenticated read API.
            options.AddPolicy(GlobalRateLimitPolicy, httpContext =>
            {
                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString()
                                   ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    });
            });
        });

        return services;
    }
}
