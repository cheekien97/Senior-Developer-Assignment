using System.ComponentModel.DataAnnotations;
using CovidAnalyticsPortal.Web.Models.ApiContracts;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CovidAnalyticsPortal.Web.Models.ViewModels;

/// <summary>
/// View model backing the Audit Trail page. Carries the filter inputs, the
/// option list for the action dropdown, and the result rows from the API.
/// </summary>
public sealed class AuditTrailViewModel
{
    /// <summary>Gets or sets the inclusive start date of the query window.</summary>
    [DataType(DataType.Date)]
    [Display(Name = "From")]
    public DateOnly? From { get; set; }

    /// <summary>Gets or sets the inclusive end date of the query window.</summary>
    [DataType(DataType.Date)]
    [Display(Name = "To")]
    public DateOnly? To { get; set; }

    /// <summary>Gets or sets the selected action, or <c>null</c> for all actions.</summary>
    [Display(Name = "Action")]
    public string? Action { get; set; }

    /// <summary>Gets or sets the selectable list of auditable actions.</summary>
    public IReadOnlyList<SelectListItem> Actions { get; set; } = [];

    /// <summary>Gets or sets the audit entries returned by the API.</summary>
    public IReadOnlyList<AuditTrailResponse> Results { get; set; } = [];

    /// <summary>Gets or sets an optional informational or error message.</summary>
    public string? Message { get; set; }
}
