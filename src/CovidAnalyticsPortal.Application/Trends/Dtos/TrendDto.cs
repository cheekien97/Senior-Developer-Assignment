namespace CovidAnalyticsPortal.Application.Trends.Dtos;

/// <summary>
/// A single point in a metric's time series, used to render historical and
/// trend charts on the client.
/// </summary>
public sealed record TrendPointDto
{
    /// <summary>Gets the date of the data point.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Gets the metric value at the date.</summary>
    public decimal Value { get; init; }
}

/// <summary>
/// Read-model describing the trend of a single metric over a period, including
/// the summary statistics (absolute and percentage change, direction) and the
/// full series of points for charting.
/// </summary>
public sealed record TrendDto
{
    /// <summary>Gets the metric the trend describes (e.g. <c>Cases</c>).</summary>
    public required string Metric { get; init; }

    /// <summary>
    /// Gets the state name the trend is scoped to, or <c>null</c> for a
    /// national trend.
    /// </summary>
    public string? StateName { get; init; }

    /// <summary>Gets the inclusive start date of the analysed period.</summary>
    public required DateOnly PeriodStart { get; init; }

    /// <summary>Gets the inclusive end date of the analysed period.</summary>
    public required DateOnly PeriodEnd { get; init; }

    /// <summary>Gets the metric value at the start of the period.</summary>
    public decimal StartValue { get; init; }

    /// <summary>Gets the metric value at the end of the period.</summary>
    public decimal EndValue { get; init; }

    /// <summary>Gets the absolute change across the period.</summary>
    public decimal ChangeValue { get; init; }

    /// <summary>Gets the percentage change across the period.</summary>
    public decimal ChangePercentage { get; init; }

    /// <summary>
    /// Gets the derived trend direction (e.g. <c>Increasing</c>,
    /// <c>Decreasing</c>, <c>Stable</c>).
    /// </summary>
    public required string Direction { get; init; }

    /// <summary>Gets the time series of points underlying the trend.</summary>
    public IReadOnlyList<TrendPointDto> Points { get; init; } = Array.Empty<TrendPointDto>();
}
