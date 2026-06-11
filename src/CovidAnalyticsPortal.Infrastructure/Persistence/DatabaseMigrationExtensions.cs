using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CovidAnalyticsPortal.Infrastructure.Persistence;

/// <summary>
/// Start-up helpers that bring the database schema up to date by applying any
/// pending EF Core migrations. Invoked once during host start-up so the portal
/// is usable without any manual database provisioning.
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies all pending migrations for the <see cref="AppDbContext"/> when
    /// the configured provider is relational. Runs inside its own service scope
    /// and logs the outcome.
    /// </summary>
    /// <param name="services">The application's root service provider.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public static async Task MigrateDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        var context = provider.GetRequiredService<AppDbContext>();
        var logger = provider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("CovidAnalyticsPortal.Infrastructure.Persistence.DatabaseMigration");

        // Non-relational providers (e.g. the in-memory provider) neither need
        // nor support relational migrations.
        if (!context.Database.IsRelational())
        {
            logger.LogInformation(
                "Skipping database migration; the configured provider is not relational");
            return;
        }

        logger.LogInformation("Applying pending database migrations");

        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Database schema is up to date");
    }
}
