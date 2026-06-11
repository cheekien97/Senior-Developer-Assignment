using CovidAnalyticsPortal.Application.Common.Interfaces;

namespace CovidAnalyticsPortal.API.Context;

/// <summary>
/// HTTP-based implementation of <see cref="ICurrentContext"/>. Reads the acting
/// user, correlation identifier, and originating IP address from the current
/// <see cref="HttpContext"/> so that downstream components (such as the audit
/// service) can enrich their output without taking a dependency on ASP.NET.
/// </summary>
public sealed class HttpCurrentContext : ICurrentContext
{
    /// <summary>
    /// The header used to carry the correlation identifier across services.
    /// </summary>
    public const string CorrelationHeader = "X-Correlation-ID";

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpCurrentContext"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The accessor for the current HTTP context.</param>
    public HttpCurrentContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string Actor =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true
            ? _httpContextAccessor.HttpContext!.User.Identity!.Name ?? "Authenticated"
            : "Anonymous";

    /// <inheritdoc />
    public string CorrelationId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null)
            {
                return "no-context";
            }

            if (context.Items.TryGetValue(CorrelationHeader, out var value) &&
                value is string correlationId &&
                !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }

            return context.TraceIdentifier;
        }
    }

    /// <inheritdoc />
    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
