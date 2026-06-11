using CovidAnalyticsPortal.Domain.Enums;

namespace CovidAnalyticsPortal.Application.Common.Interfaces;

/// <summary>
/// Application-level contract for recording entries in the portal's audit
/// trail. Abstracting the audit sink keeps the use cases unaware of how or
/// where audit data is persisted (Dependency Inversion).
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Records an auditable event asynchronously.
    /// </summary>
    /// <param name="action">The action that was performed.</param>
    /// <param name="description">A human-readable description of the event.</param>
    /// <param name="entityName">The name of the targeted entity or resource, if any.</param>
    /// <param name="parameters">A serialized representation of associated parameters, if any.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    Task RecordAsync(
        AuditAction action,
        string description,
        string? entityName = null,
        string? parameters = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Supplies ambient information about the current request — the acting user, a
/// correlation identifier, and the originating IP address — to components that
/// need it (such as the audit service). Implemented in the presentation/host
/// layer where the request context is available.
/// </summary>
public interface ICurrentContext
{
    /// <summary>
    /// Gets the identity of the current actor (e.g. user name, "Anonymous", or
    /// "System").
    /// </summary>
    string Actor { get; }

    /// <summary>
    /// Gets the correlation identifier for the current request.
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Gets the IP address the current request originated from, if available.
    /// </summary>
    string? IpAddress { get; }
}
