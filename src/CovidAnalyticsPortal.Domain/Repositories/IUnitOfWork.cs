using CovidAnalyticsPortal.Domain.Entities;

namespace CovidAnalyticsPortal.Domain.Repositories;

/// <summary>
/// Coordinates work across multiple repositories within a single business
/// transaction and exposes a single atomic commit. The Unit of Work ensures
/// that all changes made through its repositories either succeed together or
/// fail together, preserving aggregate consistency.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets the repository for national, country-level daily statistics.
    /// </summary>
    IRepository<CovidStatistic> CovidStatistics { get; }

    /// <summary>
    /// Gets the repository for state-level daily statistics.
    /// </summary>
    IRepository<StateStatistic> StateStatistics { get; }

    /// <summary>
    /// Gets the repository for computed trend records.
    /// </summary>
    IRepository<TrendRecord> TrendRecords { get; }

    /// <summary>
    /// Gets the repository for audit trail entries.
    /// </summary>
    IRepository<AuditTrail> AuditTrails { get; }

    /// <summary>
    /// Persists all pending changes made through the unit's repositories as a
    /// single atomic transaction.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The number of state entries written to the underlying store.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
