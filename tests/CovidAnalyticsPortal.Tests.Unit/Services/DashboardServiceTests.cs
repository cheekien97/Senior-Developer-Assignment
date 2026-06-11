using System.Linq.Expressions;
using CovidAnalyticsPortal.Application.Services;
using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Domain.Repositories;
using CovidAnalyticsPortal.Domain.ValueObjects;
using CovidAnalyticsPortal.Tests.Unit.TestHelpers;
using FluentAssertions;
using Moq;

namespace CovidAnalyticsPortal.Tests.Unit.Services;

/// <summary>
/// Unit tests for <see cref="DashboardService"/>, exercising aggregation and
/// edge cases with a mocked <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class DashboardServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private readonly Mock<IRepository<CovidStatistic>> _nationalRepo = new();
    private readonly Mock<IRepository<StateStatistic>> _stateRepo = new();
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _unitOfWork.SetupGet(u => u.CovidStatistics).Returns(_nationalRepo.Object);
        _unitOfWork.SetupGet(u => u.StateStatistics).Returns(_stateRepo.Object);
        _sut = new DashboardService(_unitOfWork.Object);
    }

    [Fact]
    public async Task BuildDashboardAsync_WithData_AggregatesLatestNationalFigures()
    {
        var period = DateRange.Create(new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 3));
        var national = new List<CovidStatistic>
        {
            TestData.National(new DateOnly(2021, 6, 1), TestData.Metrics(newCases: 10, cumulativeCases: 100, cumulativeDeaths: 5, recovered: 3)),
            TestData.National(new DateOnly(2021, 6, 3), TestData.Metrics(newCases: 30, cumulativeCases: 300, activeCases: 150, newDeaths: 7, cumulativeDeaths: 20, recovered: 9)),
            TestData.National(new DateOnly(2021, 6, 2), TestData.Metrics(newCases: 20, cumulativeCases: 200, recovered: 6)),
        };

        SetupNational(national);
        SetupStateBreakdown(new DateOnly(2021, 6, 3), new List<StateStatistic>
        {
            TestData.State("SGR", new DateOnly(2021, 6, 3), TestData.Metrics(newCases: 25)),
            TestData.State("JHR", new DateOnly(2021, 6, 3), TestData.Metrics(newCases: 5)),
        });

        var result = await _sut.BuildDashboardAsync(period);

        result.AsOfDate.Should().Be(new DateOnly(2021, 6, 3));
        result.TotalCases.Should().Be(300);
        result.ActiveCases.Should().Be(150);
        result.TotalDeaths.Should().Be(20);
        result.NewCasesToday.Should().Be(30);
        result.NewDeathsToday.Should().Be(7);
        result.TotalRecovered.Should().Be(3 + 6 + 9);
        result.StateBreakdown.Should().HaveCount(2);
        result.StateBreakdown[0].StateCode.Should().Be("SGR", "the breakdown is ordered by new cases descending");
    }

    [Fact]
    public async Task BuildDashboardAsync_WithNoData_ReturnsEmptySummaryForPeriod()
    {
        var period = DateRange.Create(new DateOnly(2021, 1, 1), new DateOnly(2021, 1, 31));
        SetupNational(new List<CovidStatistic>());

        var result = await _sut.BuildDashboardAsync(period);

        result.PeriodStart.Should().Be(period.Start);
        result.PeriodEnd.Should().Be(period.End);
        result.AsOfDate.Should().Be(period.End);
        result.TotalCases.Should().Be(0);
        result.StateBreakdown.Should().BeEmpty();
        _stateRepo.Verify(
            r => r.FindAsync(It.IsAny<Expression<Func<StateStatistic, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "no state breakdown is built when there is no national data");
    }

    private void SetupNational(List<CovidStatistic> data) =>
        _nationalRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CovidStatistic, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<CovidStatistic, bool>> predicate, CancellationToken _) =>
                data.Where(predicate.Compile()).ToList());

    private void SetupStateBreakdown(DateOnly date, List<StateStatistic> data) =>
        _stateRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<StateStatistic, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<StateStatistic, bool>> predicate, CancellationToken _) =>
                data.Where(predicate.Compile()).ToList());
}
