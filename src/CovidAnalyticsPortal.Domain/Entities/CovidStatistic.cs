using CovidAnalyticsPortal.Domain.Common;
using CovidAnalyticsPortal.Domain.Exceptions;
using CovidAnalyticsPortal.Domain.ValueObjects;

namespace CovidAnalyticsPortal.Domain.Entities;

/// <summary>
/// Aggregate root representing the national, country-level COVID-19 statistics
/// for a single calendar day. Acts as the consistency boundary for daily
/// national figures and enforces its own invariants through factory and
/// mutation methods rather than exposing public setters.
/// </summary>
public sealed class CovidStatistic : AuditableEntity
{
    /// <summary>
    /// Gets the calendar day the statistics describe.
    /// </summary>
    public DateOnly Date { get; private set; }

    /// <summary>
    /// Gets the case metrics captured for the day.
    /// </summary>
    public CaseMetrics Metrics { get; private set; } = CaseMetrics.Zero;

    // Private parameterless constructor for the persistence provider (EF Core).
    private CovidStatistic()
    {
    }

    private CovidStatistic(Guid id, DateOnly date, CaseMetrics metrics)
        : base(id)
    {
        Date = date;
        Metrics = metrics;
    }

    /// <summary>
    /// Creates a new national daily statistic.
    /// </summary>
    /// <param name="date">The calendar day the statistics describe.</param>
    /// <param name="metrics">The case metrics for the day.</param>
    /// <param name="utcNow">The current UTC time, used for audit stamping.</param>
    /// <returns>A new, validated <see cref="CovidStatistic"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="metrics"/> is null.</exception>
    public static CovidStatistic Create(DateOnly date, CaseMetrics metrics, DateTime utcNow)
    {
        DomainException.ThrowIf(metrics is null, "Case metrics must be provided.");

        var statistic = new CovidStatistic(Guid.NewGuid(), date, metrics!);
        statistic.MarkCreated(utcNow);
        return statistic;
    }

    /// <summary>
    /// Replaces the case metrics for this day, for example when a later
    /// ingestion run supersedes a provisional figure.
    /// </summary>
    /// <param name="metrics">The updated case metrics.</param>
    /// <param name="utcNow">The current UTC time, used for audit stamping.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="metrics"/> is null.</exception>
    public void UpdateMetrics(CaseMetrics metrics, DateTime utcNow)
    {
        DomainException.ThrowIf(metrics is null, "Case metrics must be provided.");

        Metrics = metrics!;
        MarkModified(utcNow);
    }
}
