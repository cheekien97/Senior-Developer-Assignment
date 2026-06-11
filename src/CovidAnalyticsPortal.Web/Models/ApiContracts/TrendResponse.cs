namespace CovidAnalyticsPortal.Web.Models.ApiContracts;

/// <summary>
/// A single point on a trend series.
/// </summary>
public sealed class TrendPointResponse
{
    /// <summary>Gets or sets the date of the data point.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets the metric value at that date.</summary>
    public long Value { get; set; }
}

/// <summary>
/// Client-side mirror of the API's trend-analysis contract.
/// </summary>
public sealed class TrendResponse
{
    /// <summary>Gets or sets the analysed metric name (e.g. <c>Cases</c>).</summary>
    public string Metric { get; set; } = string.Empty;

    /// <summary>Gets or sets the state name, or <c>null</c> for national trends.</summary>
    public string? StateName { get; set; }

    /// <summary>Gets or sets the inclusive start date of the analysed period.</summary>
    public DateOnly PeriodStart { get; set; }

    /// <summary>Gets or sets the inclusive end date of the analysed period.</summary>
    public DateOnly PeriodEnd { get; set; }

    /// <summary>Gets or sets the metric value at the start of the period.</summary>
    public long StartValue { get; set; }

    /// <summary>Gets or sets the metric value at the end of the period.</summary>
    public long EndValue { get; set; }

    /// <summary>Gets or sets the absolute change across the period.</summary>
    public decimal ChangeValue { get; set; }

    /// <summary>Gets or sets the percentage change across the period.</summary>
    public decimal ChangePercentage { get; set; }

    /// <summary>Gets or sets the qualitative direction (Increasing, Decreasing, Stable).</summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>Gets or sets the ordered series of data points.</summary>
    public List<TrendPointResponse> Points { get; set; } = [];
}
