using CovidAnalyticsPortal.Application.Common.Interfaces;
using CovidAnalyticsPortal.Infrastructure.BackgroundServices;
using CovidAnalyticsPortal.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CovidAnalyticsPortal.Tests.Integration;

/// <summary>
/// Integration tests for the COVID data ingestion path. They run the real
/// <see cref="CovidDataImporter"/> against a SQLite database whose schema is
/// created by applying the EF Core migration, using a stubbed
/// <see cref="IMohDataProvider"/> in place of the live MoH feed. This verifies
/// the migration produces a usable schema and that ingestion persists,
/// de-duplicates, and updates records correctly.
/// </summary>
public sealed class DataSyncIngestionTests
{
    private static readonly DateOnly Day1 = new(2021, 6, 1);
    private static readonly DateOnly Day2 = new(2021, 6, 2);
    private static readonly DateOnly Day3 = new(2021, 6, 3);

    private static readonly DateTime FixedUtcNow = new(2021, 6, 4, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ImportAsync_OnEmptyDatabase_PersistsNationalAndStateRecords()
    {
        using var connection = OpenSharedConnection();

        var provider = new StubMohDataProvider(SampleNational(), SampleState());

        var imported = await RunImportAsync(connection, provider, applyMigration: true);

        imported.Should().Be(7); // 3 national + 4 valid state (1 unknown state skipped)

        await using var context = CreateContext(connection);
        context.CovidStatistics.Count().Should().Be(3);
        context.StateStatistics.Count().Should().Be(4);
    }

    [Fact]
    public async Task ImportAsync_RunTwiceWithSameData_IsIdempotent()
    {
        using var connection = OpenSharedConnection();

        var first = await RunImportAsync(
            connection, new StubMohDataProvider(SampleNational(), SampleState()), applyMigration: true);
        first.Should().Be(7);

        // A second run over identical data must not create or change anything.
        var second = await RunImportAsync(
            connection, new StubMohDataProvider(SampleNational(), SampleState()), applyMigration: false);
        second.Should().Be(0);

        await using var context = CreateContext(connection);
        context.CovidStatistics.Count().Should().Be(3);
        context.StateStatistics.Count().Should().Be(4);
    }

    [Fact]
    public async Task ImportAsync_WhenMetricsChange_UpdatesExistingRecord()
    {
        using var connection = OpenSharedConnection();

        await RunImportAsync(
            connection, new StubMohDataProvider(SampleNational(), SampleState()), applyMigration: true);

        // Same date, revised figures: should update exactly one national row.
        var revisedNational = new List<MohDailyRecord>
        {
            new(Day1, null, NewCases: 999, CumulativeCases: 999, ActiveCases: 10, Recovered: 5, NewDeaths: 1, CumulativeDeaths: 1),
        };

        var changes = await RunImportAsync(
            connection, new StubMohDataProvider(revisedNational, Array.Empty<MohDailyRecord>()), applyMigration: false);

        changes.Should().Be(1);

        await using var context = CreateContext(connection);
        var day1 = context.CovidStatistics.Single(s => s.Date == Day1);
        day1.Metrics.NewCases.Should().Be(999);
        day1.LastModifiedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ImportAsync_WithEmptyFeed_PersistsNothingAndDoesNotThrow()
    {
        using var connection = OpenSharedConnection();

        var provider = new StubMohDataProvider(
            Array.Empty<MohDailyRecord>(), Array.Empty<MohDailyRecord>());

        var imported = await RunImportAsync(connection, provider, applyMigration: true);

        imported.Should().Be(0);

        await using var context = CreateContext(connection);
        context.CovidStatistics.Count().Should().Be(0);
        context.StateStatistics.Count().Should().Be(0);
    }

    private static async Task<int> RunImportAsync(
        SqliteConnection connection,
        IMohDataProvider provider,
        bool applyMigration)
    {
        // A fresh context per run mirrors the scoped-per-run production design
        // and avoids change-tracker bleed between successive imports.
        await using var context = CreateContext(connection);

        if (applyMigration)
        {
            await context.Database.MigrateAsync();
        }

        var unitOfWork = new UnitOfWork(context, NullLogger<UnitOfWork>.Instance);
        var importer = new CovidDataImporter(
            provider,
            unitOfWork,
            new FixedClock(FixedUtcNow),
            NullLogger<CovidDataImporter>.Instance);

        return await importer.ImportAsync();
    }

    private static SqliteConnection OpenSharedConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static AppDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        return new AppDbContext(options);
    }

    private static IReadOnlyList<MohDailyRecord> SampleNational() => new List<MohDailyRecord>
    {
        new(Day1, null, NewCases: 100, CumulativeCases: 100, ActiveCases: 80, Recovered: 10, NewDeaths: 2, CumulativeDeaths: 2),
        new(Day2, null, NewCases: 120, CumulativeCases: 220, ActiveCases: 90, Recovered: 15, NewDeaths: 3, CumulativeDeaths: 5),
        new(Day3, null, NewCases: 140, CumulativeCases: 360, ActiveCases: 95, Recovered: 20, NewDeaths: 4, CumulativeDeaths: 9),
    };

    private static IReadOnlyList<MohDailyRecord> SampleState() => new List<MohDailyRecord>
    {
        // State names (as published by MoH) and codes both resolve via StateCode.
        new(Day1, "Selangor", NewCases: 50, CumulativeCases: 50, ActiveCases: 40, Recovered: 5, NewDeaths: 1, CumulativeDeaths: 1),
        new(Day2, "Selangor", NewCases: 60, CumulativeCases: 110, ActiveCases: 45, Recovered: 6, NewDeaths: 1, CumulativeDeaths: 2),
        new(Day1, "Johor", NewCases: 30, CumulativeCases: 30, ActiveCases: 20, Recovered: 3, NewDeaths: 0, CumulativeDeaths: 0),
        new(Day2, "Johor", NewCases: 35, CumulativeCases: 65, ActiveCases: 22, Recovered: 4, NewDeaths: 1, CumulativeDeaths: 1),
        // Unknown subdivision: must be skipped without error.
        new(Day1, "Atlantis", NewCases: 1, CumulativeCases: 1, ActiveCases: 1, Recovered: 0, NewDeaths: 0, CumulativeDeaths: 0),
    };

    private sealed class StubMohDataProvider : IMohDataProvider
    {
        private readonly IReadOnlyList<MohDailyRecord> _national;
        private readonly IReadOnlyList<MohDailyRecord> _state;

        public StubMohDataProvider(
            IReadOnlyList<MohDailyRecord> national,
            IReadOnlyList<MohDailyRecord> state)
        {
            _national = national;
            _state = state;
        }

        public Task<IReadOnlyList<MohDailyRecord>> GetNationalDailyAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(_national);

        public Task<IReadOnlyList<MohDailyRecord>> GetStateDailyAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(_state);
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }

        public DateOnly Today => DateOnly.FromDateTime(UtcNow);
    }
}
