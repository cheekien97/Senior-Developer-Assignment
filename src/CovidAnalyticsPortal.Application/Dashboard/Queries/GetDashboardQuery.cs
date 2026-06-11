using CovidAnalyticsPortal.Application.Common.Interfaces;
using CovidAnalyticsPortal.Application.Dashboard.Dtos;
using CovidAnalyticsPortal.Domain.ValueObjects;
using MediatR;

namespace CovidAnalyticsPortal.Application.Dashboard.Queries;

/// <summary>
/// CQRS query that requests the dashboard summary for an optional reporting
/// period. When the dates are omitted, the handler defaults to the most recent
/// 30-day window. Being a query, it never mutates state.
/// </summary>
/// <param name="From">The inclusive start date, or <c>null</c> to use the default window.</param>
/// <param name="To">The inclusive end date, or <c>null</c> to use the current date.</param>
public sealed record GetDashboardQuery(DateOnly? From = null, DateOnly? To = null)
    : IRequest<DashboardDto>;

/// <summary>
/// Handles <see cref="GetDashboardQuery"/> by resolving the reporting period
/// and delegating aggregation to the <see cref="IDashboardService"/>.
/// </summary>
public sealed class GetDashboardQueryHandler
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private const int DefaultWindowDays = 30;

    private readonly IDashboardService _dashboardService;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDashboardQueryHandler"/> class.
    /// </summary>
    /// <param name="dashboardService">The service that assembles the dashboard.</param>
    /// <param name="dateTimeProvider">The clock used to resolve default dates.</param>
    public GetDashboardQueryHandler(
        IDashboardService dashboardService,
        IDateTimeProvider dateTimeProvider)
    {
        _dashboardService = dashboardService;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<DashboardDto> Handle(
        GetDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var end = request.To ?? _dateTimeProvider.Today;
        var start = request.From ?? end.AddDays(-(DefaultWindowDays - 1));

        var period = DateRange.Create(start, end);

        return await _dashboardService
            .BuildDashboardAsync(period, cancellationToken)
            .ConfigureAwait(false);
    }
}
