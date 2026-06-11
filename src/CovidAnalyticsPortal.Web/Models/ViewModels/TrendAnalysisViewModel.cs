using System.ComponentModel.DataAnnotations;
using CovidAnalyticsPortal.Web.Models.ApiContracts;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CovidAnalyticsPortal.Web.Models.ViewModels;

/// <summary>
/// View model backing the Trend Analysis page. Carries the filter inputs, the
/// option lists for the metric and state dropdowns, and the analysed trend.
/// </summary>
public sealed class TrendAnalysisViewModel
{
    /// <summary>Gets or sets the metric to analyse (matches the API's MetricType enum names).</summary>
    [Required]
    [Display(Name = "Metric")]
    public string Metric { get; set; } = "Cases";

    /// <summary>Gets or sets the inclusive start date of the analysis window.</summary>
    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "From")]
    public DateOnly From { get; set; }

    /// <summary>Gets or sets the inclusive end date of the analysis window.</summary>
    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "To")]
    public DateOnly To { get; set; }

    /// <summary>Gets or sets the selected state code, or <c>null</c> for a national trend.</summary>
    [Display(Name = "State")]
    public string? State { get; set; }

    /// <summary>Gets or sets the selectable list of metrics.</summary>
    public IReadOnlyList<SelectListItem> Metrics { get; set; } = [];

    /// <summary>Gets or sets the selectable list of Malaysian states.</summary>
    public IReadOnlyList<SelectListItem> States { get; set; } = [];

    /// <summary>Gets or sets the analysed trend returned by the API.</summary>
    public TrendResponse? Trend { get; set; }

    /// <summary>Gets a value indicating whether the analysis has been executed.</summary>
    public bool Searched { get; set; }

    /// <summary>Gets or sets an optional informational or error message.</summary>
    public string? Message { get; set; }
}
