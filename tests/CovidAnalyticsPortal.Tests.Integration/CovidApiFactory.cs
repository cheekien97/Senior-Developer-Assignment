using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Domain.ValueObjects;
using CovidAnalyticsPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CovidAnalyticsPortal.Tests.Integration;

/// <summary>
/// A <see cref="WebApplicationFactory{TEntryPoint}"/> that boots the real API
/// host but swaps the configured database for a private in-memory SQLite
/// instance, then seeds it with deterministic data. This lets the endpoint
/// tests run the full HTTP pipeline (routing, versioning, validation, exception
/// handling, MediatR, services, repositories) without an external database.
/// </summary>
public sealed class CovidApiFactory : WebApplicationFactory<Program>
{
    /// <summary>The most recent seeded national date.</summary>
    public static readonly DateOnly LatestDate = new(2021, 6, 30);

    /// <summary>The earliest seeded national date.</summary>
    public static readonly DateOnly EarliestDate = new(2021, 6, 1);

    private static readonly DateTime SeedUtcNow = new(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    private SqliteConnection? _connection;
    private bool _seeded;

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        // Keep the endpoint tests hermetic: disable the outbound MoH data
        // synchronisation so no network calls are made during the run. Each
        // test seeds its own deterministic data instead.
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CovidDataSync:Enabled"] = "false",
            });
        });

        builder.ConfigureServices(services =>
        {
            RemoveAppDbContext(services);

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    /// <summary>
    /// Ensures the schema exists and the deterministic seed data is present.
    /// Idempotent so it can be called from each test's setup.
    /// </summary>
    public void EnsureSeeded()
    {
        if (_seeded)
        {
            return;
        }

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();

        if (!context.CovidStatistics.Any())
        {
            SeedNational(context);
            SeedStates(context);
            context.SaveChanges();
        }

        _seeded = true;
    }

    private static void SeedNational(AppDbContext context)
    {
        var cumulativeCases = 1_000L;
        var cumulativeDeaths = 50L;

        for (var date = EarliestDate; date <= LatestDate; date = date.AddDays(1))
        {
            var dayIndex = date.DayNumber - EarliestDate.DayNumber;
            var newCases = 100 + (dayIndex * 10);
            var newDeaths = 2 + dayIndex;
            cumulativeCases += newCases;
            cumulativeDeaths += newDeaths;

            var metrics = CaseMetrics.Create(
                newCases,
                cumulativeCases,
                activeCases: 500 + dayIndex,
                recovered: 40,
                newDeaths,
                cumulativeDeaths);

            context.CovidStatistics.Add(CovidStatistic.Create(date, metrics, SeedUtcNow));
        }
    }

    private static void SeedStates(AppDbContext context)
    {
        foreach (var code in new[] { "SGR", "JHR", "PNG" })
        {
            for (var date = EarliestDate; date <= LatestDate; date = date.AddDays(1))
            {
                var dayIndex = date.DayNumber - EarliestDate.DayNumber;
                var metrics = CaseMetrics.Create(
                    newCases: 20 + dayIndex,
                    cumulativeCases: 200 + (dayIndex * 20),
                    activeCases: 100 + dayIndex,
                    recovered: 10,
                    newDeaths: 1,
                    cumulativeDeaths: 10 + dayIndex);

                context.StateStatistics.Add(
                    StateStatistic.Create(StateCode.Create(code), date, metrics, SeedUtcNow));
            }
        }
    }

    private static void RemoveAppDbContext(IServiceCollection services)
    {
        var descriptors = services
            .Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(AppDbContext))
            .ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}
