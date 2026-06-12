namespace CovidAnalyticsPortal.Application.Audit.Dtos;

/// <summary>
/// Read-model describing a single audit trail entry. Flat,
/// serialization-friendly shape intended for transfer across the API boundary
/// and consumption by the MVC presentation layer.
/// </summary>
public sealed record AuditTrailDto
{
    /// <summary>Gets the unique identifier of the audit entry.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the name of the action that was performed (e.g. <c>ViewDashboard</c>).</summary>
    public required string Action { get; init; }

    /// <summary>Gets the human-readable description of the audited event.</summary>
    public required string Description { get; init; }

    /// <summary>Gets the identity of the actor that triggered the event.</summary>
    public required string Actor { get; init; }

    /// <summary>Gets the correlation identifier linking the entry to a request.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>Gets the name of the targeted entity or resource, if any.</summary>
    public string? EntityName { get; init; }

    /// <summary>Gets the serialized parameters associated with the action, if any.</summary>
    public string? Parameters { get; init; }

    /// <summary>Gets the originating IP address, if available.</summary>
    public string? IpAddress { get; init; }

    /// <summary>Gets the UTC timestamp at which the event occurred.</summary>
    public required DateTime TimestampUtc { get; init; }
}
