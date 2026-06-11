using System.ComponentModel.DataAnnotations;

namespace CovidAnalyticsPortal.Infrastructure.BackgroundServices;

/// <summary>
/// Strongly-typed configuration for the <see cref="CovidDataSyncBackgroundService"/>,
/// bound from the <c>CovidDataSync</c> configuration section. Controls whether
/// the ingestion runs, how soon after start-up it first runs, and how often it
/// repeats.
/// </summary>
public sealed class CovidDataSyncOptions
{
    /// <summary>The configuration section name these options bind to.</summary>
    public const string SectionName = "CovidDataSync";

    /// <summary>
    /// Gets or sets a value indicating whether the background ingestion is
    /// enabled. Disabled in automated tests so they remain hermetic.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the delay, in seconds, before the first synchronisation run
    /// after application start-up. A small delay lets the host finish booting
    /// before any outbound calls are made.
    /// </summary>
    [Range(0, 3600)]
    public int InitialDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the interval, in hours, between successive synchronisation
    /// runs. The MoH feed updates at most daily, so a value of several hours is
    /// ample.
    /// </summary>
    [Range(1, 168)]
    public int IntervalHours { get; set; } = 12;
}
