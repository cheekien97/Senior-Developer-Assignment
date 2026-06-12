namespace CovidAnalyticsPortal.Web.Models.ApiContracts;

/// <summary>
/// Client-side mirror of the API's audit trail entry contract.
/// </summary>
public sealed class AuditTrailResponse
{
    /// <summary>Gets or sets the unique identifier of the audit entry.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of the action that was performed.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable description of the event.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the identity of the actor that triggered the event.</summary>
    public string Actor { get; set; } = string.Empty;

    /// <summary>Gets or sets the correlation identifier linking the entry to a request.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the targeted entity or resource, if any.</summary>
    public string? EntityName { get; set; }

    /// <summary>Gets or sets the serialized parameters associated with the action, if any.</summary>
    public string? Parameters { get; set; }

    /// <summary>Gets or sets the originating IP address, if available.</summary>
    public string? IpAddress { get; set; }

    /// <summary>Gets or sets the UTC timestamp at which the event occurred.</summary>
    public DateTime TimestampUtc { get; set; }
}
