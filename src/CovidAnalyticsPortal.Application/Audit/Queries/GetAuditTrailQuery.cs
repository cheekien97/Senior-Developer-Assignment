using CovidAnalyticsPortal.Application.Audit.Dtos;
using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Domain.Enums;
using CovidAnalyticsPortal.Domain.Repositories;
using MediatR;

namespace CovidAnalyticsPortal.Application.Audit.Queries;

/// <summary>
/// CQRS query that retrieves audit trail entries, optionally filtered by an
/// action and/or an inclusive date range, ordered most-recent first and capped
/// to a maximum number of rows. Being a query, it never mutates state.
/// </summary>
/// <param name="From">The inclusive start date, or <c>null</c> for no lower bound.</param>
/// <param name="To">The inclusive end date, or <c>null</c> for no upper bound.</param>
/// <param name="Action">The action to filter by, or <c>null</c> for all actions.</param>
/// <param name="MaxResults">The maximum number of entries to return.</param>
public sealed record GetAuditTrailQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    AuditAction? Action = null,
    int MaxResults = 100) : IRequest<IReadOnlyList<AuditTrailDto>>;

/// <summary>
/// Handles <see cref="GetAuditTrailQuery"/> by reading from the audit trail
/// repository through the Unit of Work and projecting the entries into the
/// presentation-friendly <see cref="AuditTrailDto"/>.
/// </summary>
public sealed class GetAuditTrailQueryHandler
    : IRequestHandler<GetAuditTrailQuery, IReadOnlyList<AuditTrailDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAuditTrailQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work providing repository access.</param>
    public GetAuditTrailQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditTrailDto>> Handle(
        GetAuditTrailQuery request,
        CancellationToken cancellationToken)
    {
        // Translate the optional DateOnly bounds into a half-open DateTime range
        // so the comparison stays translatable by the persistence provider.
        var start = request.From?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue;
        var end = request.To.HasValue
            ? request.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue)
            : DateTime.MaxValue;
        var action = request.Action;

        var entries = await _unitOfWork.AuditTrails
            .FindAsync(
                e => e.TimestampUtc >= start
                    && e.TimestampUtc < end
                    && (!action.HasValue || e.Action == action.Value),
                cancellationToken)
            .ConfigureAwait(false);

        return entries
            .OrderByDescending(e => e.TimestampUtc)
            .Take(request.MaxResults)
            .Select(MapToDto)
            .ToList();
    }

    private static AuditTrailDto MapToDto(AuditTrail entry) => new()
    {
        Id = entry.Id,
        Action = entry.Action.ToString(),
        Description = entry.Description,
        Actor = entry.Actor,
        CorrelationId = entry.CorrelationId,
        EntityName = entry.EntityName,
        Parameters = entry.Parameters,
        IpAddress = entry.IpAddress,
        TimestampUtc = entry.TimestampUtc,
    };
}
