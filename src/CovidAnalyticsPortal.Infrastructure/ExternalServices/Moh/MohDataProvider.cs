using System.Globalization;
using CovidAnalyticsPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CovidAnalyticsPortal.Infrastructure.ExternalServices.Moh;

/// <summary>
/// Typed-<see cref="HttpClient"/> implementation of <see cref="IMohDataProvider"/>
/// that retrieves and normalises COVID-19 datasets published by the Ministry of
/// Health Malaysia. Responses are cached in-memory to reduce load on the
/// upstream feed, and the merged cases/deaths data is projected into the
/// application's <see cref="MohDailyRecord"/> contract. Resilience (retry,
/// timeout, circuit breaking) is supplied by the registered HTTP pipeline.
/// </summary>
public sealed class MohDataProvider : IMohDataProvider
{
    private const string NationalCacheKey = "moh:national-daily";
    private const string StateCacheKey = "moh:state-daily";

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly MohApiOptions _options;
    private readonly ILogger<MohDataProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MohDataProvider"/> class.
    /// </summary>
    /// <param name="httpClient">The configured, resilient HTTP client.</param>
    /// <param name="cache">The in-memory cache for ingested datasets.</param>
    /// <param name="options">The validated MoH integration options.</param>
    /// <param name="logger">The logger for structured diagnostics.</param>
    public MohDataProvider(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<MohApiOptions> options,
        ILogger<MohDataProvider> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MohDailyRecord>> GetNationalDailyAsync(
        CancellationToken cancellationToken = default) =>
        GetOrAddAsync(
            NationalCacheKey,
            () => LoadNationalAsync(cancellationToken));

    /// <inheritdoc />
    public Task<IReadOnlyList<MohDailyRecord>> GetStateDailyAsync(
        CancellationToken cancellationToken = default) =>
        GetOrAddAsync(
            StateCacheKey,
            () => LoadStateAsync(cancellationToken));

    private async Task<IReadOnlyList<MohDailyRecord>> GetOrAddAsync(
        string cacheKey,
        Func<Task<IReadOnlyList<MohDailyRecord>>> factory)
    {
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<MohDailyRecord>? cached) && cached is not null)
        {
            _logger.LogDebug("MoH cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        _logger.LogDebug("MoH cache miss for {CacheKey}; fetching from source", cacheKey);

        var data = await factory().ConfigureAwait(false);

        _cache.Set(
            cacheKey,
            data,
            new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(_options.CacheMinutes),
            });

        return data;
    }

    private async Task<IReadOnlyList<MohDailyRecord>> LoadNationalAsync(
        CancellationToken cancellationToken)
    {
        var casesRows = await FetchCsvAsync(_options.NationalCasesPath, cancellationToken)
            .ConfigureAwait(false);
        var deathsRows = await FetchCsvAsync(_options.NationalDeathsPath, cancellationToken)
            .ConfigureAwait(false);

        var deathsByDate = deathsRows
            .Select(r => (Date: ParseDate(CsvReader.GetString(r, "date")), Row: r))
            .Where(x => x.Date.HasValue)
            .GroupBy(x => x.Date!.Value)
            .ToDictionary(g => g.Key, g => g.First().Row);

        long cumulativeCases = 0;
        long cumulativeDeaths = 0;
        var records = new List<MohDailyRecord>();

        foreach (var row in casesRows.OrderBy(r => CsvReader.GetString(r, "date")))
        {
            var date = ParseDate(CsvReader.GetString(row, "date"));
            if (date is null)
            {
                continue;
            }

            var newCases = CsvReader.GetLong(row, "cases_new");
            var recovered = CsvReader.GetLong(row, "cases_recovered");
            var active = CsvReader.GetLong(row, "cases_active");

            var newDeaths = deathsByDate.TryGetValue(date.Value, out var deathRow)
                ? CsvReader.GetLong(deathRow, "deaths_new")
                : 0;

            cumulativeCases += newCases;
            cumulativeDeaths += newDeaths;

            records.Add(new MohDailyRecord(
                date.Value,
                StateNameOrCode: null,
                NewCases: newCases,
                CumulativeCases: cumulativeCases,
                ActiveCases: active,
                Recovered: recovered,
                NewDeaths: newDeaths,
                CumulativeDeaths: cumulativeDeaths));
        }

        _logger.LogInformation("Loaded {Count} national MoH daily records", records.Count);
        return records;
    }

    private async Task<IReadOnlyList<MohDailyRecord>> LoadStateAsync(
        CancellationToken cancellationToken)
    {
        var casesRows = await FetchCsvAsync(_options.StateCasesPath, cancellationToken)
            .ConfigureAwait(false);
        var deathsRows = await FetchCsvAsync(_options.StateDeathsPath, cancellationToken)
            .ConfigureAwait(false);

        var deathsByKey = deathsRows
            .Select(r => (
                Date: ParseDate(CsvReader.GetString(r, "date")),
                State: CsvReader.GetString(r, "state"),
                Row: r))
            .Where(x => x.Date.HasValue && x.State is not null)
            .GroupBy(x => (x.Date!.Value, x.State!))
            .ToDictionary(g => g.Key, g => g.First().Row);

        // Cumulative totals are tracked per state.
        var cumulativeCasesByState = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var cumulativeDeathsByState = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var records = new List<MohDailyRecord>();

        foreach (var row in casesRows
                     .OrderBy(r => CsvReader.GetString(r, "date"))
                     .ThenBy(r => CsvReader.GetString(r, "state")))
        {
            var date = ParseDate(CsvReader.GetString(row, "date"));
            var state = CsvReader.GetString(row, "state");
            if (date is null || state is null)
            {
                continue;
            }

            var newCases = CsvReader.GetLong(row, "cases_new");
            var recovered = CsvReader.GetLong(row, "cases_recovered");
            var active = CsvReader.GetLong(row, "cases_active");

            var newDeaths = deathsByKey.TryGetValue((date.Value, state), out var deathRow)
                ? CsvReader.GetLong(deathRow, "deaths_new")
                : 0;

            cumulativeCasesByState.TryGetValue(state, out var cumCases);
            cumulativeDeathsByState.TryGetValue(state, out var cumDeaths);
            cumCases += newCases;
            cumDeaths += newDeaths;
            cumulativeCasesByState[state] = cumCases;
            cumulativeDeathsByState[state] = cumDeaths;

            records.Add(new MohDailyRecord(
                date.Value,
                StateNameOrCode: state,
                NewCases: newCases,
                CumulativeCases: cumCases,
                ActiveCases: active,
                Recovered: recovered,
                NewDeaths: newDeaths,
                CumulativeDeaths: cumDeaths));
        }

        _logger.LogInformation("Loaded {Count} state-level MoH daily records", records.Count);
        return records;
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> FetchCsvAsync(
        string relativePath,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Requesting MoH dataset {RelativePath}", relativePath);

        using var response = await _httpClient
            .GetAsync(relativePath, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var content = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        return CsvReader.Parse(content);
    }

    private static DateOnly? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }
}
