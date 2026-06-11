namespace CovidAnalyticsPortal.Domain.Enums;

/// <summary>
/// Identifies a measurable COVID-19 metric that can be aggregated, charted, or
/// analysed for trends across the portal.
/// </summary>
public enum MetricType
{
    /// <summary>New confirmed COVID-19 cases.</summary>
    Cases = 0,

    /// <summary>Currently active (unresolved) COVID-19 cases.</summary>
    ActiveCases = 1,

    /// <summary>Recovered COVID-19 cases.</summary>
    Recovered = 2,

    /// <summary>Deaths attributed to COVID-19.</summary>
    Deaths = 3,
}
