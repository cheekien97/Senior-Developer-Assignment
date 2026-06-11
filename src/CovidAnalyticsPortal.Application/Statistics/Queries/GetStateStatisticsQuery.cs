using CovidAnalyticsPortal.Application.Common.Interfaces;
using CovidAnalyticsPortal.Application.Statistics.Dtos;
using CovidAnalyticsPortal.Domain.ValueObjects;
using MediatR;

namespace CovidAnalyticsPortal.Application.Statistics.Queries;

/// <summary>
/// CQRS query that requests the daily statistics for a state (or all states
/// when none is specified) over an inclusive date range. Supports the portal's
/// country/state filtering feature.
/// </summary>
/// <param name="From">The inclusive start date of the range.</param>
/// <param name="To">The inclusive end date of the range.</param>
/// <param name="State">The state code or name to filter by, or <c>null</c> for all states.</param>
public sealed record GetStateStatisticsQuery(DateOnly From, DateOnly To, string? State = null)
    : IRequest<IReadOnlyList<StateStatisticDto>>;

/// <summary>
/// Handles <see cref="GetStateStatisticsQuery"/> by translating the primitive
/// inputs into domain value objects and delegating to the
/// <see cref="IAnalyticsService"/>.
/// </summary>
public sealed class GetStateStatisticsQueryHandler
    : IRequestHandler<GetStateStatisticsQuery, IReadOnlyList<StateStatisticDto>>
{
    private readonly IAnalyticsService _analyticsService;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="GetStateStatisticsQueryHandler"/> class.
    /// </summary>
    /// <param name="analyticsService">The analytics service providing the data.</param>
    public GetStateStatisticsQueryHandler(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StateStatisticDto>> Handle(
        GetStateStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var period = DateRange.Create(request.From, request.To);

        StateCode? state = null;
        if (!string.IsNullOrWhiteSpace(request.State))
        {
            // Validation has already confirmed the value is parseable.
            StateCode.TryParse(request.State, out state);
        }

        return await _analyticsService
            .GetStateStatisticsAsync(state, period, cancellationToken)
            .ConfigureAwait(false);
    }
}
