using CovidAnalyticsPortal.Domain.Common;
using CovidAnalyticsPortal.Domain.Enums;
using CovidAnalyticsPortal.Domain.Exceptions;
using CovidAnalyticsPortal.Domain.ValueObjects;

namespace CovidAnalyticsPortal.Domain.Entities;

/// <summary>
/// Aggregate root representing a computed trend for a single metric over a
/// period, optionally scoped to a state. Encapsulates the trend mathematics
/// (absolute change, percentage change, and direction) so the calculation lives
/// in the domain and cannot drift between callers.
/// </summary>
public sealed class TrendRecord : AuditableEntity
{
    /// <summary>
    /// A change at or below this percentage magnitude is treated as
    /// <see cref="TrendDirection.Stable"/>.
    /// </summary>
    private const decimal StabilityThresholdPercent = 1.0m;

    /// <summary>
    /// Gets the state the trend is scoped to, or <c>null</c> when the trend is
    /// national.
    /// </summary>
    public StateCode? State { get; private set; }

    /// <summary>
    /// Gets the metric this trend describes.
    /// </summary>
    public MetricType Metric { get; private set; }

    /// <summary>
    /// Gets the period the trend was computed over.
    /// </summary>
    public DateRange Period { get; private set; } = null!;

    /// <summary>
    /// Gets the metric value at the start of the period.
    /// </summary>
    public decimal StartValue { get; private set; }

    /// <summary>
    /// Gets the metric value at the end of the period.
    /// </summary>
    public decimal EndValue { get; private set; }

    /// <summary>
    /// Gets the absolute change in the metric across the period
    /// (<see cref="EndValue"/> minus <see cref="StartValue"/>).
    /// </summary>
    public decimal ChangeValue { get; private set; }

    /// <summary>
    /// Gets the percentage change in the metric across the period.
    /// </summary>
    public decimal ChangePercentage { get; private set; }

    /// <summary>
    /// Gets the derived direction of the trend.
    /// </summary>
    public TrendDirection Direction { get; private set; }

    // Private parameterless constructor for the persistence provider (EF Core).
    private TrendRecord()
    {
    }

    private TrendRecord(
        Guid id,
        StateCode? state,
        MetricType metric,
        DateRange period,
        decimal startValue,
        decimal endValue)
        : base(id)
    {
        State = state;
        Metric = metric;
        Period = period;
        StartValue = startValue;
        EndValue = endValue;
        ChangeValue = endValue - startValue;
        ChangePercentage = CalculatePercentage(startValue, endValue);
        Direction = DeriveDirection(ChangePercentage);
    }

    /// <summary>
    /// Gets a value indicating whether the trend is national (not scoped to a
    /// specific state).
    /// </summary>
    public bool IsNational => State is null;

    /// <summary>
    /// Creates a new trend record, computing the change, percentage, and
    /// direction from the supplied start and end values.
    /// </summary>
    /// <param name="metric">The metric the trend describes.</param>
    /// <param name="period">The period the trend covers.</param>
    /// <param name="startValue">The metric value at the start of the period.</param>
    /// <param name="endValue">The metric value at the end of the period.</param>
    /// <param name="utcNow">The current UTC time, used for audit stamping.</param>
    /// <param name="state">The state to scope the trend to, or <c>null</c> for a national trend.</param>
    /// <returns>A new, validated <see cref="TrendRecord"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="period"/> is null.</exception>
    public static TrendRecord Create(
        MetricType metric,
        DateRange period,
        decimal startValue,
        decimal endValue,
        DateTime utcNow,
        StateCode? state = null)
    {
        DomainException.ThrowIf(period is null, "A trend period must be provided.");

        var record = new TrendRecord(Guid.NewGuid(), state, metric, period!, startValue, endValue);
        record.MarkCreated(utcNow);
        return record;
    }

    private static decimal CalculatePercentage(decimal startValue, decimal endValue)
    {
        if (startValue == 0m)
        {
            // No baseline to compare against: report 0% when flat, otherwise 100% growth from zero.
            return endValue == 0m ? 0m : 100m;
        }

        return Math.Round((endValue - startValue) / Math.Abs(startValue) * 100m, 2);
    }

    private static TrendDirection DeriveDirection(decimal changePercentage)
    {
        if (Math.Abs(changePercentage) <= StabilityThresholdPercent)
        {
            return TrendDirection.Stable;
        }

        return changePercentage > 0m
            ? TrendDirection.Increasing
            : TrendDirection.Decreasing;
    }
}
