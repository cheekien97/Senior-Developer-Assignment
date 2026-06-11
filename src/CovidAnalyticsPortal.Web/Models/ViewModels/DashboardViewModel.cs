using System.ComponentModel.DataAnnotations;
using CovidAnalyticsPortal.Web.Models.ApiContracts;

namespace CovidAnalyticsPortal.Web.Models.ViewModels;

/// <summary>
/// View model backing the Dashboard page. Wraps the API summary together with
/// the date filter and a flag indicating whether data was available.
/// </summary>
public sealed class DashboardViewModel
{
    /// <summary>Gets or sets the inclusive start date of the reporting window.</summary>
    [DataType(DataType.Date)]
    [Display(Name = "From")]
    public DateOnly? From { get; set; }

    /// <summary>Gets or sets the inclusive end date of the reporting window.</summary>
    [DataType(DataType.Date)]
    [Display(Name = "To")]
    public DateOnly? To { get; set; }

    /// <summary>Gets or sets the dashboard summary returned by the API.</summary>
    public DashboardResponse? Summary { get; set; }

    /// <summary>Gets a value indicating whether any data is available to render.</summary>
    public bool HasData => Summary is not null;

    /// <summary>Gets or sets an optional message shown when the API returns no data or fails.</summary>
    public string? Message { get; set; }
}
