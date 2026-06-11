using System.ComponentModel.DataAnnotations;

namespace CovidAnalyticsPortal.Infrastructure.ExternalServices.Moh;

/// <summary>
/// Strongly-typed, validated configuration for the Ministry of Health (MoH)
/// open-data integration. Binding configuration to a dedicated options class
/// keeps magic strings out of the code and allows the URLs to be overridden per
/// environment without recompilation.
/// </summary>
public sealed class MohApiOptions
{
    /// <summary>
    /// The configuration section name these options bind to.
    /// </summary>
    public const string SectionName = "MohApi";

    /// <summary>
    /// Gets or sets the base address of the MoH open-data source. The default
    /// points at the official MoH public COVID-19 dataset repository.
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } =
        "https://raw.githubusercontent.com/MoH-Malaysia/covid19-public/main/";

    /// <summary>
    /// Gets or sets the relative path to the national daily cases dataset.
    /// </summary>
    [Required]
    public string NationalCasesPath { get; set; } = "epidemic/cases_malaysia.csv";

    /// <summary>
    /// Gets or sets the relative path to the state-level daily cases dataset.
    /// </summary>
    [Required]
    public string StateCasesPath { get; set; } = "epidemic/cases_state.csv";

    /// <summary>
    /// Gets or sets the relative path to the national daily deaths dataset.
    /// </summary>
    [Required]
    public string NationalDeathsPath { get; set; } = "epidemic/deaths_malaysia.csv";

    /// <summary>
    /// Gets or sets the relative path to the state-level daily deaths dataset.
    /// </summary>
    [Required]
    public string StateDeathsPath { get; set; } = "epidemic/deaths_state.csv";

    /// <summary>
    /// Gets or sets the per-request timeout, in seconds.
    /// </summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the number of transient-failure retries the resilient HTTP
    /// pipeline should attempt.
    /// </summary>
    [Range(0, 10)]
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the sliding cache duration, in minutes, applied to ingested
    /// MoH data.
    /// </summary>
    [Range(1, 1440)]
    public int CacheMinutes { get; set; } = 60;
}
