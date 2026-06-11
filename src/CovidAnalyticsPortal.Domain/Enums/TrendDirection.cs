namespace CovidAnalyticsPortal.Domain.Enums;

/// <summary>
/// Describes the overall direction of a metric over a period, derived by
/// comparing the metric's start and end values.
/// </summary>
public enum TrendDirection
{
    /// <summary>The metric remained effectively unchanged over the period.</summary>
    Stable = 0,

    /// <summary>The metric increased over the period.</summary>
    Increasing = 1,

    /// <summary>The metric decreased over the period.</summary>
    Decreasing = 2,
}
