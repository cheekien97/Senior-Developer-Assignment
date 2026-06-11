namespace CovidAnalyticsPortal.API.Middleware;

/// <summary>
/// Adds a baseline set of security response headers to every response,
/// mitigating common client-side risks (MIME sniffing, clickjacking, referrer
/// leakage, and legacy content-type guessing). Aligned with the OWASP Secure
/// Headers project and applied uniformly so individual endpoints need no
/// per-action configuration.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityHeadersMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Attaches the security headers before the response is written, then
    /// invokes the rest of the pipeline.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Prevent browsers from MIME-sniffing a response away from the
            // declared content type.
            headers["X-Content-Type-Options"] = "nosniff";

            // Disallow the API responses from being framed (clickjacking).
            headers["X-Frame-Options"] = "DENY";

            // Do not leak the request URL to other origins.
            headers["Referrer-Policy"] = "no-referrer";

            // Disable powerful browser features by default for any HTML error
            // pages the API might surface.
            headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";

            // A restrictive content-security policy; the JSON API serves no
            // active content, so default-deny is appropriate.
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

            // Remove server fingerprinting where present.
            headers.Remove("Server");

            return Task.CompletedTask;
        });

        await _next(context).ConfigureAwait(false);
    }
}
