namespace CovidAnalyticsPortal.Domain.Enums;

/// <summary>
/// Enumerates the auditable actions tracked by the portal's audit trail. Each
/// value represents a meaningful user or system interaction that must be
/// recorded for accountability and traceability.
/// </summary>
public enum AuditAction
{
    /// <summary>A user viewed the dashboard summary.</summary>
    ViewDashboard = 0,

    /// <summary>A user viewed detailed statistics.</summary>
    ViewStatistics = 1,

    /// <summary>A user viewed trend analysis.</summary>
    ViewTrends = 2,

    /// <summary>A user viewed historical charts.</summary>
    ViewHistory = 3,

    /// <summary>A user applied a country/state or date filter.</summary>
    ApplyFilter = 4,

    /// <summary>A user exported data.</summary>
    ExportData = 5,

    /// <summary>The system ingested data from the external MoH feed.</summary>
    IngestData = 6,

    /// <summary>The system recorded a handled error condition.</summary>
    SystemError = 7,
}
