using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CovidAnalyticsPortal.Web.Models.ApiContracts;
using Microsoft.Extensions.Options;

namespace CovidAnalyticsPortal.Web.Services;

/// <summary>
/// Typed <see cref="HttpClient"/> implementation of <see cref="ICovidApiClient"/>.
/// Builds versioned routes, performs JSON content negotiation and maps
/// not-found / no-content responses to empty results.
/// </summary>
public sealed class CovidApiClient : ICovidApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<CovidApiClient> _logger;
    private readonly string _apiVersion;

    /// <summary>
    /// Initialises a new instance of the <see cref="CovidApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The configured typed client.</param>
    /// <param name="options">The bound API options.</param>
    /// <param name="logger">The logger.</param>
    public CovidApiClient(
        HttpClient httpClient,
        IOptions<CovidApiOptions> options,
        ILogger<CovidApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiVersion = options.Value.ApiVersion;
    }

    /// <inheritdoc />
    public async Task<DashboardResponse?> GetDashboardAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("from", from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("to", to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));

        var uri = $"api/v{_apiVersion}/dashboard{query}";
        return await GetJsonAsync<DashboardResponse>(uri, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StateStatisticResponse>> GetStateStatisticsAsync(
        DateOnly from,
        DateOnly to,
        string? state,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("from", from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("to", to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("state", state));

        var uri = $"api/v{_apiVersion}/analytics/statistics{query}";
        var result = await GetJsonAsync<List<StateStatisticResponse>>(uri, cancellationToken).ConfigureAwait(false);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<TrendResponse?> GetTrendAsync(
        string metric,
        DateOnly from,
        DateOnly to,
        string? state,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(
            ("metric", metric),
            ("from", from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("to", to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            ("state", state));

        var uri = $"api/v{_apiVersion}/analytics/trends{query}";
        return await GetJsonAsync<TrendResponse>(uri, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T?> GetJsonAsync<T>(string uri, CancellationToken cancellationToken)
    {
        using var response = await _httpClient
            .GetAsync(uri, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<T>(SerializerOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string BuildQuery(params (string Key, string? Value)[] parameters)
    {
        var pairs = parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}")
            .ToArray();

        return pairs.Length == 0 ? string.Empty : "?" + string.Join("&", pairs);
    }
}
