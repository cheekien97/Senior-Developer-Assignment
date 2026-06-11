using CovidAnalyticsPortal.Application.Common.Interfaces;
using CovidAnalyticsPortal.Application.Dashboard.Dtos;
using CovidAnalyticsPortal.Application.Statistics.Dtos;
using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Domain.Repositories;
using CovidAnalyticsPortal.Domain.ValueObjects;

namespace CovidAnalyticsPortal.Application.Services;

/// <summary>
/// Default implementation of <see cref="IDashboardService"/>. Aggregates
/// national and state-level data from the persistence layer (accessed through
/// the Unit of Work) into a single dashboard read-model. All data access is
/// asynchronous.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardService"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work providing repository access.</param>
    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<DashboardDto> BuildDashboardAsync(
        DateRange period,
        CancellationToken cancellationToken = default)
    {
        var nationalStats = await _unitOfWork.CovidStatistics
            .FindAsync(s => s.Date >= period.Start && s.Date <= period.End, cancellationToken)
            .ConfigureAwait(false);

        var ordered = nationalStats
            .OrderBy(s => s.Date)
            .ToList();

        if (ordered.Count == 0)
        {
            return new DashboardDto
            {
                AsOfDate = period.End,
                PeriodStart = period.Start,
                PeriodEnd = period.End,
            };
        }

        var latest = ordered[^1];

        var totalRecovered = ordered.Sum(s => s.Metrics.Recovered);

        var stateBreakdown = await BuildStateBreakdownAsync(latest.Date, cancellationToken)
            .ConfigureAwait(false);

        return new DashboardDto
        {
            AsOfDate = latest.Date,
            PeriodStart = period.Start,
            PeriodEnd = period.End,
            TotalCases = latest.Metrics.CumulativeCases,
            TotalDeaths = latest.Metrics.CumulativeDeaths,
            ActiveCases = latest.Metrics.ActiveCases,
            TotalRecovered = totalRecovered,
            NewCasesToday = latest.Metrics.NewCases,
            NewDeathsToday = latest.Metrics.NewDeaths,
            StateBreakdown = stateBreakdown,
        };
    }

    private async Task<IReadOnlyList<StateStatisticDto>> BuildStateBreakdownAsync(
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var stateStats = await _unitOfWork.StateStatistics
            .FindAsync(s => s.Date == date, cancellationToken)
            .ConfigureAwait(false);

        return stateStats
            .OrderByDescending(s => s.Metrics.NewCases)
            .Select(MapToDto)
            .ToList();
    }

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
