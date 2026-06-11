using CovidAnalyticsPortal.Application.Common.Interfaces;
using CovidAnalyticsPortal.Domain.Repositories;
using CovidAnalyticsPortal.Infrastructure.Audit;
using CovidAnalyticsPortal.Infrastructure.BackgroundServices;
using CovidAnalyticsPortal.Infrastructure.ExternalServices.Moh;
using CovidAnalyticsPortal.Infrastructure.Persistence;
using CovidAnalyticsPortal.Infrastructure.Persistence.Repositories;
using CovidAnalyticsPortal.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace CovidAnalyticsPortal.Infrastructure;

/// <summary>
/// Composition-root helpers for the infrastructure layer. Registers persistence
/// (EF Core, repositories, Unit of Work), the resilient MoH HTTP integration,
/// in-memory caching, the system clock, and the audit service. The host wires
/// these in by calling <see cref="AddInfrastructure"/>.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all infrastructure services with the dependency-injection
    /// container.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddMohIntegration(configuration);

        services.AddMemoryCache();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IAuditService, AuditService>();

        services.AddDataSynchronisation(configuration);

        return services;
    }

    private static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? "Data Source=covidportal.db";

        services.AddDbContext<AppDbContext>(options =>
        {
            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        // Open-generic repository registration for any direct IRepository<T> consumers.
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    private static IServiceCollection AddMohIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<MohApiOptions>()
            .Bind(configuration.GetSection(MohApiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration
            .GetSection(MohApiOptions.SectionName)
            .Get<MohApiOptions>() ?? new MohApiOptions();

        services.AddHttpClient<IMohDataProvider, MohDataProvider>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "text/csv,text/plain");
            client.DefaultRequestHeaders.Add("User-Agent", "CovidAnalyticsPortal/1.0");
        })
        .AddStandardResilienceHandler(resilience =>
        {
            resilience.Retry.MaxRetryAttempts = options.RetryCount;
            resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            resilience.TotalRequestTimeout.Timeout =
                TimeSpan.FromSeconds(options.TimeoutSeconds * (options.RetryCount + 1));
            resilience.CircuitBreaker.SamplingDuration =
                TimeSpan.FromSeconds(options.TimeoutSeconds * 2);
        });

        return services;
    }

    private static IServiceCollection AddDataSynchronisation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<CovidDataSyncOptions>()
            .Bind(configuration.GetSection(CovidDataSyncOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<CovidDataImporter>();
        services.AddHostedService<CovidDataSyncBackgroundService>();

        return services;
    }
}
