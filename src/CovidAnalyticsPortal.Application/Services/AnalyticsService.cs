using CovidAnalyticsPortal.Application.Common.Interfaces;
using CovidAnalyticsPortal.Application.Statistics.Dtos;
using CovidAnalyticsPortal.Application.Trends.Dtos;
using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Domain.Enums;
using CovidAnalyticsPortal.Domain.Repositories;
using CovidAnalyticsPortal.Domain.ValueObjects;

namespace CovidAnalyticsPortal.Application.Services;

/// <summary>
/// Default implementation of <see cref="IAnalyticsService"/>. Provides
/// state-statistics retrieval and trend analysis by combining persisted data
/// with the domain's <see cref="TrendRecord"/> calculations. All operations are
/// asynchronous and cancellation-aware.
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyticsService"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work providing repository access.</param>
    /// <param name="dateTimeProvider">The clock abstraction used for audit stamping.</param>
    public AnalyticsService(IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StateStatisticDto>> GetStateStatisticsAsync(
        StateCode? state,
        DateRange period,
        CancellationToken cancellationToken = default)
    {
        var statistics = await _unitOfWork.StateStatistics
            .FindAsync(
                s => s.Date >= period.Start
                     && s.Date <= period.End
                     && (state == null || s.State == state),
                cancellationToken)
            .ConfigureAwait(false);

        return statistics
            .OrderBy(s => s.State.Code)
            .ThenBy(s => s.Date)
            .Select(MapToDto)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<TrendDto> AnalyzeTrendAsync(
        MetricType metric,
        DateRange period,
        StateCode? state,
        CancellationToken cancellationToken = default)
    {
        var points = state is null
            ? await BuildNationalSeriesAsync(metric, period, cancellationToken).ConfigureAwait(false)
            : await BuildStateSeriesAsync(metric, period, state, cancellationToken).ConfigureAwait(false);

        var startValue = points.Count > 0 ? points[0].Value : 0m;
        var endValue = points.Count > 0 ? points[^1].Value : 0m;

        var trend = TrendRecord.Create(
            metric,
            period,
            startValue,
            endValue,
            _dateTimeProvider.UtcNow,
            state);

        return new TrendDto
        {
            Metric = metric.ToString(),
            StateName = state?.Name,
            PeriodStart = period.Start,
            PeriodEnd = period.End,
            StartValue = trend.StartValue,
            EndValue = trend.EndValue,
            ChangeValue = trend.ChangeValue,
            ChangePercentage = trend.ChangePercentage,
            Direction = trend.Direction.ToString(),
            Points = points,
        };
    }

    private async Task<IReadOnlyList<TrendPointDto>> BuildNationalSeriesAsync(
        MetricType metric,
        DateRange period,
        CancellationToken cancellationToken)
    {
        var statistics = await _unitOfWork.CovidStatistics
            .FindAsync(s => s.Date >= period.Start && s.Date <= period.End, cancellationToken)
            .ConfigureAwait(false);

        return statistics
            .OrderBy(s => s.Date)
            .Select(s => new TrendPointDto
            {
                Date = s.Date,
                Value = SelectMetric(s.Metrics, metric),
            })
            .ToList();
    }

    private async Task<IReadOnlyList<TrendPointDto>> BuildStateSeriesAsync(
        MetricType metric,
        DateRange period,
        StateCode state,
        CancellationToken cancellationToken)
    {
        var statistics = await _unitOfWork.StateStatistics
            .FindAsync(
                s => s.State == state && s.Date >= period.Start && s.Date <= period.End,
                cancellationToken)
            .ConfigureAwait(false);

        return statistics
            .OrderBy(s => s.Date)
            .Select(s => new TrendPointDto
            {
                Date = s.Date,
                Value = SelectMetric(s.Metrics, metric),
            })
            .ToList();
    }

    private static decimal SelectMetric(CaseMetrics metrics, MetricType metric) => metric switch
    {
        MetricType.Cases => metrics.NewCases,
        MetricType.ActiveCases => metrics.ActiveCases,
        MetricType.Recovered => metrics.Recovered,
        MetricType.Deaths => metrics.NewDeaths,
        _ => 0m,
    };

    private static StateStatisticDto MapToDto(StateStatistic statistic) => new()
    {
        StateCode = statistic.State.Code,
        StateName = statistic.State.Name,
        Date = statistic.Date,
        NewCases = statistic.Metrics.NewCases,
        CumulativeCases = statistic.Metrics.CumulativeCases,
        ActiveCases = statistic.Metrics.ActiveCases,
        Recovered = statistic.Metrics.Recovered,
        NewDeaths = statistic.Metrics.NewDeaths,
        CumulativeDeaths = statistic.Metrics.CumulativeDeaths,
    };
}
