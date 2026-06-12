using System.Net;

namespace CovidAnalyticsPortal.Tests.Unit.TestHelpers;

/// <summary>
/// A stub <see cref="HttpMessageHandler"/> that returns canned responses keyed
/// by the request's relative path, so <c>MohDataProvider</c> can be tested
/// without any real network access.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly IReadOnlyDictionary<string, string> _responsesByPath;
    private readonly HttpStatusCode _statusCode;

    public StubHttpMessageHandler(
        IReadOnlyDictionary<string, string> responsesByPath,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responsesByPath = responsesByPath;
        _statusCode = statusCode;
    }

    /// <summary>Gets the number of requests this handler has served.</summary>
    public int RequestCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestCount++;

        var path = request.RequestUri!.AbsolutePath.TrimStart('/');
        var match = _responsesByPath
            .FirstOrDefault(kvp => path.EndsWith(kvp.Key, StringComparison.OrdinalIgnoreCase));

        var body = match.Value ?? string.Empty;

        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(body),
            RequestMessage = request,
        };

        return Task.FromResult(response);
    }
}
