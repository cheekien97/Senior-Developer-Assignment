using System.Linq.Expressions;
using CovidAnalyticsPortal.Application.Services;
using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Domain.Enums;
using CovidAnalyticsPortal.Domain.Repositories;
using CovidAnalyticsPortal.Domain.ValueObjects;
using CovidAnalyticsPortal.Tests.Unit.TestHelpers;
using FluentAssertions;
using Moq;

namespace CovidAnalyticsPortal.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="AnalyticsService"/>, covering state-statistics
/// retrieval and trend analysis (national and state-scoped).
/// </summary>
public sealed class AnalyticsServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<CovidStatistic>> _nationalRepo = new();
    private readonly Mock<IRepository<StateStatistic>> _stateRepo = new();
    private readonly AnalyticsService _sut;

    public AnalyticsServiceTests()
    {
        _unitOfWork.SetupGet(u => u.CovidStatistics).Returns(_nationalRepo.Object);
        _unitOfWork.SetupGet(u => u.StateStatistics).Returns(_stateRepo.Object);
        _sut = new AnalyticsService(_unitOfWork.Object, new FixedDateTimeProvider(TestData.UtcNow));
    }

    [Fact]
    public async Task GetStateStatisticsAsync_WithoutStateFilter_ReturnsAllOrdered()
    {
        var period = DateRange.Create(new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 2));
        SetupStates(new List<StateStatistic>
        {
            TestData.State("SGR", new DateOnly(2021, 6, 1)),
            TestData.State("JHR", new DateOnly(2021, 6, 1)),
        });

        var result = await _sut.GetStateStatisticsAsync(null, period);

        result.Should().HaveCount(2);
        result[0].StateCode.Should().Be("JHR", "results are ordered by state code");
    }

    [Fact]
    public async Task GetStateStatisticsAsync_WithStateFilter_ReturnsOnlyThatState()
    {
        var period = DateRange.Create(new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 2));
        SetupStates(new List<StateStatistic>
        {
            TestData.State("SGR", new DateOnly(2021, 6, 1)),
            TestData.State("JHR", new DateOnly(2021, 6, 1)),
        });

        var result = await _sut.GetStateStatisticsAsync(StateCode.Create("SGR"), period);

        result.Should().ContainSingle()
            .Which.StateCode.Should().Be("SGR");
    }

    [Fact]
    public async Task AnalyzeTrendAsync_National_ComputesIncreasingTrend()
    {
        var period = DateRange.Create(new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 3));
        SetupNational(new List<CovidStatistic>
        {
            TestData.National(new DateOnly(2021, 6, 1), TestData.Metrics(newCases: 100)),
            TestData.National(new DateOnly(2021, 6, 3), TestData.Metrics(newCases: 200)),
            TestData.National(new DateOnly(2021, 6, 2), TestData.Metrics(newCases: 150)),
        });

        var result = await _sut.AnalyzeTrendAsync(MetricType.Cases, period, state: null);

        result.Metric.Should().Be("Cases");
        result.StateName.Should().BeNull();
        result.Points.Should().HaveCount(3);
        result.StartValue.Should().Be(100);
        result.EndValue.Should().Be(200);
        result.ChangeValue.Should().Be(100);
        result.Direction.Should().Be(nameof(TrendDirection.Increasing));
    }

    [Fact]
    public async Task AnalyzeTrendAsync_State_UsesStateSeriesAndReportsName()
    {
        var period = DateRange.Create(new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 2));
        var state = StateCode.Create("SGR");
        SetupStates(new List<StateStatistic>
        {
            TestData.State("SGR", new DateOnly(2021, 6, 1), TestData.Metrics(newDeaths: 10)),
            TestData.State("SGR", new DateOnly(2021, 6, 2), TestData.Metrics(newDeaths: 4)),
            TestData.State("JHR", new DateOnly(2021, 6, 2), TestData.Metrics(newDeaths: 99)),
        });

        var result = await _sut.AnalyzeTrendAsync(MetricType.Deaths, period, state);

        result.StateName.Should().Be("Selangor");
        result.Points.Should().HaveCount(2, "only the requested state's points are included");
        result.StartValue.Should().Be(10);
        result.EndValue.Should().Be(4);
        result.Direction.Should().Be(nameof(TrendDirection.Decreasing));
    }

    [Fact]
    public async Task AnalyzeTrendAsync_WithNoData_ReturnsZeroedStableTrend()
    {
        var period = DateRange.Create(new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 2));
        SetupNational(new List<CovidStatistic>());

        var result = await _sut.AnalyzeTrendAsync(MetricType.Cases, period, state: null);

        result.Points.Should().BeEmpty();
        result.StartValue.Should().Be(0);
        result.EndValue.Should().Be(0);
        result.Direction.Should().Be(nameof(TrendDirection.Stable));
    }

    private void SetupNational(List<CovidStatistic> data) =>
        _nationalRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CovidStatistic, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<CovidStatistic, bool>> predicate, CancellationToken _) =>
                data.Where(predicate.Compile()).ToList());

    private void SetupStates(List<StateStatistic> data) =>
        _stateRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StateStatistic, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<StateStatistic, bool>> predicate, CancellationToken _) =>
                data.Where(predicate.Compile()).ToList());
}
