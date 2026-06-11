using CovidAnalyticsPortal.API.Context;
using Serilog.Context;

namespace CovidAnalyticsPortal.API.Middleware;

/// <summary>
/// Ensures every request carries a correlation identifier. The middleware reads
/// an inbound <c>X-Correlation-ID</c> header (or generates one), stores it for
/// the duration of the request, pushes it into the Serilog
/// <see cref="LogContext"/> so it appears on every log line, and echoes it back
/// on the response for end-to-end traceability.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Processes the request, attaching the correlation identifier to context,
    /// logs, and the response.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[HttpCurrentContext.CorrelationHeader] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HttpCurrentContext.CorrelationHeader] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context).ConfigureAwait(false);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HttpCurrentContext.CorrelationHeader, out var header) &&
            !string.IsNullOrWhiteSpace(header))
        {
            return header.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }
}
