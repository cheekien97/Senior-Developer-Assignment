using CovidAnalyticsPortal.Application.Common.Interfaces;
using CovidAnalyticsPortal.Application.Statistics.Dtos;
using CovidAnalyticsPortal.Application.Statistics.Queries;
using CovidAnalyticsPortal.Application.Trends.Dtos;
using CovidAnalyticsPortal.Application.Trends.Queries;
using CovidAnalyticsPortal.Domain.Enums;
using CovidAnalyticsPortal.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace CovidAnalyticsPortal.Tests.Unit.Application.Handlers;

/// <summary>
/// Unit tests for <see cref="GetStateStatisticsQueryHandler"/> and
/// <see cref="GetTrendAnalysisQueryHandler"/>, verifying state-code parsing and
/// delegation to the analytics service.
/// </summary>
public sealed class AnalyticsQueryHandlerTests
{
    private readonly Mock<IAnalyticsService> _service = new();

    [Fact]
    public async Task StateStatistics_WithStateFilter_PassesParsedStateCode()
    {
        StateCode? captured = null;
        _service
            .Setup(s => s.GetStateStatisticsAsync(
                It.IsAny<StateCode?>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .Callback<StateCode?, DateRange, CancellationToken>((state, _, _) => captured = state)
            .ReturnsAsync(Array.Empty<StateStatisticDto>());

        var handler = new GetStateStatisticsQueryHandler(_service.Object);
        var query = new GetStateStatisticsQuery(new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 10), "SGR");

        await handler.Handle(query, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Code.Should().Be("SGR");
    }

    [Fact]
    public async Task StateStatistics_WithoutStateFilter_PassesNull()
    {
        StateCode? captured = StateCode.Create("SGR");
        _service
            .Setup(s => s.GetStateStatisticsAsync(
                It.IsAny<StateCode?>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .Callback<StateCode?, DateRange, CancellationToken>((state, _, _) => captured = state)
            .ReturnsAsync(Array.Empty<StateStatisticDto>());

        var handler = new GetStateStatisticsQueryHandler(_service.Object);
        var query = new GetStateStatisticsQuery(new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 10));

        await handler.Handle(query, CancellationToken.None);

        captured.Should().BeNull();
    }

    [Fact]
    public async Task TrendAnalysis_DelegatesWithMetricAndState()
    {
        MetricType capturedMetric = default;
        StateCode? capturedState = null;
        _service
            .Setup(s => s.AnalyzeTrendAsync(
                It.IsAny<MetricType>(), It.IsAny<DateRange>(), It.IsAny<StateCode?>(), It.IsAny<CancellationToken>()))
            .Callback<MetricType, DateRange, StateCode?, CancellationToken>(
                (metric, _, state, _) => { capturedMetric = metric; capturedState = state; })
            .ReturnsAsync(new TrendDto
            {
                Metric = MetricType.Deaths.ToString(),
                PeriodStart = new DateOnly(2021, 6, 1),
                PeriodEnd = new DateOnly(2021, 6, 10),
                Direction = TrendDirection.Stable.ToString(),
            });

        var handler = new GetTrendAnalysisQueryHandler(_service.Object);
        var query = new GetTrendAnalysisQuery(
            MetricType.Deaths, new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 10), "JHR");

        var result = await handler.Handle(query, CancellationToken.None);

        capturedMetric.Should().Be(MetricType.Deaths);
        capturedState!.Code.Should().Be("JHR");
        result.Metric.Should().Be(MetricType.Deaths.ToString());
    }
}
