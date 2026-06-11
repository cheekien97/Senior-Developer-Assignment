using CovidAnalyticsPortal.Domain.Common;
using CovidAnalyticsPortal.Domain.Enums;
using CovidAnalyticsPortal.Domain.Exceptions;

namespace CovidAnalyticsPortal.Domain.Entities;

/// <summary>
/// Immutable aggregate root representing a single entry in the portal's audit
/// trail. Once created, an audit entry never changes — this append-only design
/// preserves the integrity and non-repudiation of the audit record. Each entry
/// captures who did what, when, and within which correlated request.
/// </summary>
public sealed class AuditTrail : Entity
{
    /// <summary>
    /// Gets the action that was performed.
    /// </summary>
    public AuditAction Action { get; private set; }

    /// <summary>
    /// Gets a human-readable description of the audited event.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the identity of the actor that triggered the event (e.g. a user
    /// name, "Anonymous", or "System" for background processes).
    /// </summary>
    public string Actor { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the correlation identifier that links this entry to a single
    /// request as it flows through the system.
    /// </summary>
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the name of the entity or resource the action targeted, if
    /// applicable.
    /// </summary>
    public string? EntityName { get; private set; }

    /// <summary>
    /// Gets a serialized representation of the parameters associated with the
    /// action (e.g. applied filters), if applicable.
    /// </summary>
    public string? Parameters { get; private set; }

    /// <summary>
    /// Gets the IP address from which the request originated, if available.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp at which the event occurred.
    /// </summary>
    public DateTime TimestampUtc { get; private set; }

    // Private parameterless constructor for the persistence provider (EF Core).
    private AuditTrail()
    {
    }

    private AuditTrail(
        Guid id,
        AuditAction action,
        string description,
        string actor,
        string correlationId,
        DateTime timestampUtc,
        string? entityName,
        string? parameters,
        string? ipAddress)
        : base(id)
    {
        Action = action;
        Description = description;
        Actor = actor;
        CorrelationId = correlationId;
        TimestampUtc = timestampUtc;
        EntityName = entityName;
        Parameters = parameters;
        IpAddress = ipAddress;
    }

    /// <summary>
    /// Creates a new, validated audit trail entry.
    /// </summary>
    /// <param name="action">The action that was performed.</param>
    /// <param name="description">A human-readable description of the event.</param>
    /// <param name="actor">The identity of the actor that triggered the event.</param>
    /// <param name="correlationId">The correlation identifier for the originating request.</param>
    /// <param name="utcNow">The UTC time at which the event occurred.</param>
    /// <param name="entityName">The name of the targeted entity or resource, if any.</param>
    /// <param name="parameters">A serialized representation of associated parameters, if any.</param>
    /// <param name="ipAddress">The originating IP address, if available.</param>
    /// <returns>A new, validated <see cref="AuditTrail"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when a required field is missing.</exception>
    public static AuditTrail Create(
        AuditAction action,
        string description,
        string actor,
        string correlationId,
        DateTime utcNow,
        string? entityName = null,
        string? parameters = null,
        string? ipAddress = null)
    {
        DomainException.ThrowIf(
            string.IsNullOrWhiteSpace(description),
            "An audit description must be provided.");
        DomainException.ThrowIf(
            string.IsNullOrWhiteSpace(actor),
            "An audit actor must be provided.");
        DomainException.ThrowIf(
            string.IsNullOrWhiteSpace(correlationId),
            "A correlation identifier must be provided.");

        return new AuditTrail(
            Guid.NewGuid(),
            action,
            description.Trim(),
            actor.Trim(),
            correlationId.Trim(),
            utcNow,
            entityName,
            parameters,
            ipAddress);
    }
}
