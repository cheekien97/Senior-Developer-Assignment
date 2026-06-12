using CovidAnalyticsPortal.Application.Common.Interfaces;
using CovidAnalyticsPortal.Infrastructure.BackgroundServices;
using CovidAnalyticsPortal.Infrastructure.Persistence;
using CovidAnalyticsPortal.Tests.Unit.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CovidAnalyticsPortal.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for <see cref="CovidDataImporter"/>, exercising the upsert logic
/// against a real in-memory SQLite store via <see cref="UnitOfWork"/> with a
/// mocked <see cref="IMohDataProvider"/>.
/// </summary>
public sealed class CovidDataImporterTests : IDisposable
{
    private readonly SqliteContextHarness _harness = new();
    private readonly Mock<IMohDataProvider> _dataProvider = new();
    private readonly FixedDateTimeProvider _clock = new(TestData.UtcNow);

    private CovidDataImporter CreateImporter(AppDbContext context)
    {
        var unitOfWork = new UnitOfWork(context, NullLogger<UnitOfWork>.Instance);
        return new CovidDataImporter(
            _dataProvider.Object,
            unitOfWork,
            _clock,
            NullLogger<CovidDataImporter>.Instance);
    }

    private static MohDailyRecord National(DateOnly date, long newCases) =>
        new(date, null, newCases, newCases, 0, 0, 0, 0);

    private static MohDailyRecord State(string state, DateOnly date, long newCases) =>
        new(date, state, newCases, newCases, 0, 0, 0, 0);

    [Fact]
    public async Task ImportAsync_WithNewRecords_InsertsNationalAndState()
    {
        _dataProvider
            .Setup(p => p.GetNationalDailyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { National(new DateOnly(2021, 6, 1), 100) });
        _dataProvider
            .Setup(p => p.GetStateDailyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { State("Selangor", new DateOnly(2021, 6, 1), 40) });

        await using var context = _harness.CreateContext();
        var importer = CreateImporter(context);

        var changes = await importer.ImportAsync();

        changes.Should().Be(2);

        await using var verifyContext = _harness.CreateContext();
        var verifyUow = new UnitOfWork(verifyContext, NullLogger<UnitOfWork>.Instance);
        (await verifyUow.CovidStatistics.ListAllAsync()).Should().ContainSingle();
        (await verifyUow.StateStatistics.ListAllAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task ImportAsync_RunTwiceWithSameData_IsIdempotent()
    {
        _dataProvider
            .Setup(p => p.GetNationalDailyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { National(new DateOnly(2021, 6, 1), 100) });
        _dataProvider
            .Setup(p => p.GetStateDailyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { State("Selangor", new DateOnly(2021, 6, 1), 40) });

        await using (var context = _harness.CreateContext())
        {
            await CreateImporter(context).ImportAsync();
        }

        int secondRunChanges;
        await using (var context = _harness.CreateContext())
        {
            secondRunChanges = await CreateImporter(context).ImportAsync();
        }

        secondRunChanges.Should().Be(0, "identical data should not produce updates");
    }

    [Fact]
    public async Task ImportAsync_WithChangedMetrics_UpdatesExistingRecord()
    {
        _dataProvider
            .Setup(p => p.GetNationalDailyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { National(new DateOnly(2021, 6, 1), 100) });
        _dataProvider
            .Setup(p => p.GetStateDailyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MohDailyRecord>());

        await using (var context = _harness.CreateContext())
        {
            await CreateImporter(context).ImportAsync();
        }

        _dataProvider
            .Setup(p => p.GetNationalDailyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { National(new DateOnly(2021, 6, 1), 250) });

        int changes;
        await using (var context = _harness.CreateContext())
        {
            changes = await CreateImporter(context).ImportAsync();
        }

        changes.Should().Be(1);

        await using var verifyContext = _harness.CreateContext();
        var verifyUow = new UnitOfWork(verifyContext, NullLogger<UnitOfWork>.Instance);
        var all = await verifyUow.CovidStatistics.ListAllAsync();
        all.Single().Metrics.NewCases.Should().Be(250);
    }

    [Fact]
    public async Task ImportAsync_SkipsUnrecognisedStates()
    {
        _dataProvider
            .Setup(p => p.GetNationalDailyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MohDailyRecord>());
        _dataProvider
            .Setup(p => p.GetStateDailyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { State("Atlantis", new DateOnly(2021, 6, 1), 5) });

        await using var context = _harness.CreateContext();
        var changes = await CreateImporter(context).ImportAsync();

        changes.Should().Be(0);
    }

    [Fact]
    public async Task ImportAsync_ClampsNegativeCorrectionsToZero()
    {
        _dataProvider
            .Setup(p => p.GetNationalDailyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new MohDailyRecord(new DateOnly(2021, 6, 1), null, -10, -5, -1, -1, -1, -1),
            });
        _dataProvider
            .Setup(p => p.GetStateDailyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MohDailyRecord>());

        await using var context = _harness.CreateContext();
        await CreateImporter(context).ImportAsync();

        await using var verifyContext = _harness.CreateContext();
        var verifyUow = new UnitOfWork(verifyContext, NullLogger<UnitOfWork>.Instance);
        var stored = (await verifyUow.CovidStatistics.ListAllAsync()).Single();
        stored.Metrics.NewCases.Should().Be(0);
        stored.Metrics.CumulativeCases.Should().Be(0);
    }

    public void Dispose() => _harness.Dispose();
}
