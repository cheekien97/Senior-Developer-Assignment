using System.ComponentModel.DataAnnotations;

namespace CovidAnalyticsPortal.Web.Services;

/// <summary>
/// Strongly-typed configuration for the backend COVID Analytics API, bound from
/// the <c>CovidApi</c> configuration section.
/// </summary>
public sealed class CovidApiOptions
{
    /// <summary>The configuration section name these options bind to.</summary>
    public const string SectionName = "CovidApi";

    /// <summary>Gets or sets the base URL of the REST API (including scheme and port).</summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the request timeout in seconds.</summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Gets or sets the API version segment used when building routes.</summary>
    [Required]
    public string ApiVersion { get; set; } = "1.0";
}
