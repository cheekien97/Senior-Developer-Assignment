using System.ComponentModel.DataAnnotations;
using CovidAnalyticsPortal.Web.Models.ApiContracts;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CovidAnalyticsPortal.Web.Models.ViewModels;

/// <summary>
/// View model backing the State Statistics page. Carries the filter inputs,
/// the option lists for the dropdowns, and the result rows from the API.
/// </summary>
public sealed class StateStatisticsViewModel
{
    /// <summary>Gets or sets the inclusive start date of the query window.</summary>
    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "From")]
    public DateOnly From { get; set; }

    /// <summary>Gets or sets the inclusive end date of the query window.</summary>
    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "To")]
    public DateOnly To { get; set; }

    /// <summary>Gets or sets the selected state code, or <c>null</c> for all states.</summary>
    [Display(Name = "State")]
    public string? State { get; set; }

    /// <summary>Gets or sets the selectable list of Malaysian states.</summary>
    public IReadOnlyList<SelectListItem> States { get; set; } = [];

    /// <summary>Gets or sets the statistic rows returned by the API.</summary>
    public IReadOnlyList<StateStatisticResponse> Results { get; set; } = [];

    /// <summary>Gets a value indicating whether the search has been executed.</summary>
    public bool Searched { get; set; }

    /// <summary>Gets or sets an optional informational or error message.</summary>
    public string? Message { get; set; }
}
