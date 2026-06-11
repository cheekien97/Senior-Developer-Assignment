using CovidAnalyticsPortal.Domain.Common;
using CovidAnalyticsPortal.Domain.Exceptions;
using CovidAnalyticsPortal.Domain.ValueObjects;

namespace CovidAnalyticsPortal.Domain.Entities;

/// <summary>
/// Aggregate root representing COVID-19 statistics for a single Malaysian state
/// or federal territory on a single calendar day. Supports the portal's
/// country/state filtering feature by associating daily case metrics with a
/// validated <see cref="StateCode"/>.
/// </summary>
public sealed class StateStatistic : AuditableEntity
{
    /// <summary>
    /// Gets the state or federal territory the statistics describe.
    /// </summary>
    public StateCode State { get; private set; } = null!;

    /// <summary>
    /// Gets the calendar day the statistics describe.
    /// </summary>
    public DateOnly Date { get; private set; }

    /// <summary>
    /// Gets the case metrics captured for the state on the day.
    /// </summary>
    public CaseMetrics Metrics { get; private set; } = CaseMetrics.Zero;

    // Private parameterless constructor for the persistence provider (EF Core).
    private StateStatistic()
    {
    }

    private StateStatistic(Guid id, StateCode state, DateOnly date, CaseMetrics metrics)
        : base(id)
    {
        State = state;
        Date = date;
        Metrics = metrics;
    }

    /// <summary>
    /// Creates a new state-level daily statistic.
    /// </summary>
    /// <param name="state">The state or federal territory.</param>
    /// <param name="date">The calendar day the statistics describe.</param>
    /// <param name="metrics">The case metrics for the day.</param>
    /// <param name="utcNow">The current UTC time, used for audit stamping.</param>
    /// <returns>A new, validated <see cref="StateStatistic"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="state"/> or <paramref name="metrics"/> is null.</exception>
    public static StateStatistic Create(
        StateCode state,
        DateOnly date,
        CaseMetrics metrics,
        DateTime utcNow)
    {
        DomainException.ThrowIf(state is null, "State must be provided.");
        DomainException.ThrowIf(metrics is null, "Case metrics must be provided.");

        var statistic = new StateStatistic(Guid.NewGuid(), state!, date, metrics!);
        statistic.MarkCreated(utcNow);
        return statistic;
    }

    /// <summary>
    /// Replaces the case metrics for this state and day, for example when a
    /// later ingestion run supersedes a provisional figure.
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
