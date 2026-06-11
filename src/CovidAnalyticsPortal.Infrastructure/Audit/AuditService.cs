using CovidAnalyticsPortal.Application.Common.Interfaces;
using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Domain.Enums;
using CovidAnalyticsPortal.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace CovidAnalyticsPortal.Infrastructure.Audit;

/// <summary>
/// Persists audit entries to the data store via the Unit of Work. Builds each
/// <see cref="AuditTrail"/> from the supplied event data enriched with ambient
/// request context (actor, correlation id, IP address). Audit failures are
/// logged but never propagated, so an auditing problem cannot break the user's
/// primary operation.
/// </summary>
public sealed class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentContext _currentContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<AuditService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditService"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work used to persist audit entries.</param>
    /// <param name="currentContext">The ambient request context.</param>
    /// <param name="dateTimeProvider">The clock used to timestamp entries.</param>
    /// <param name="logger">The logger for diagnostics.</param>
    public AuditService(
        IUnitOfWork unitOfWork,
        ICurrentContext currentContext,
        IDateTimeProvider dateTimeProvider,
        ILogger<AuditService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentContext = currentContext;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RecordAsync(
        AuditAction action,
        string description,
        string? entityName = null,
        string? parameters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = AuditTrail.Create(
                action,
                description,
                _currentContext.Actor,
                _currentContext.CorrelationId,
                _dateTimeProvider.UtcNow,
                entityName,
                parameters,
                _currentContext.IpAddress);

            await _unitOfWork.AuditTrails.AddAsync(entry, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Recorded audit entry {Action} for correlation {CorrelationId}",
                action,
                _currentContext.CorrelationId);
        }
        catch (Exception exception)
        {
            // Auditing must never break the primary request flow.
            _logger.LogError(
                exception,
                "Failed to record audit entry {Action}: {Description}",
                action,
                description);
        }
    }
}
