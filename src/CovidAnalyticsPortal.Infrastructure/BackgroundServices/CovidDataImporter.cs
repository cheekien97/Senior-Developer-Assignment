using CovidAnalyticsPortal.Application.Common.Interfaces;
using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Domain.Repositories;
using CovidAnalyticsPortal.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CovidAnalyticsPortal.Infrastructure.BackgroundServices;

/// <summary>
/// Performs a single COVID-19 data synchronisation: it pulls the latest
/// national and state daily records from the Ministry of Health feed (via
/// <see cref="IMohDataProvider"/>) and upserts them into the data store through
/// the <see cref="IUnitOfWork"/>. Kept separate from the hosting/scheduling
/// concern so the import logic can be exercised directly in tests.
/// </summary>
public sealed class CovidDataImporter
{
    private readonly IMohDataProvider _dataProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<CovidDataImporter> _logger;

    /// <summary>
    /// Initialises a new instance of the <see cref="CovidDataImporter"/> class.
    /// </summary>
    /// <param name="dataProvider">The external MoH data provider.</param>
    /// <param name="unitOfWork">The unit of work used to persist records.</param>
    /// <param name="dateTimeProvider">The clock used for audit stamping.</param>
    /// <param name="logger">The logger.</param>
    public CovidDataImporter(
        IMohDataProvider dataProvider,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ILogger<CovidDataImporter> logger)
    {
        _dataProvider = dataProvider;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Imports the latest national and state datasets, returning the number of
    /// records inserted or updated.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The number of national plus state records that were upserted.</returns>
    public async Task<int> ImportAsync(CancellationToken cancellationToken = default)
    {
        var nationalChanges = await ImportNationalAsync(cancellationToken).ConfigureAwait(false);
        var stateChanges = await ImportStateAsync(cancellationToken).ConfigureAwait(false);

        var affected = await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "COVID data import complete: {National} national and {State} state record(s) upserted ({Affected} change(s) saved)",
            nationalChanges,
            stateChanges,
            affected);

        return nationalChanges + stateChanges;
    }

    private async Task<int> ImportNationalAsync(CancellationToken cancellationToken)
    {
        var records = await _dataProvider.GetNationalDailyAsync(cancellationToken).ConfigureAwait(false);
        if (records.Count == 0)
        {
            _logger.LogWarning("MoH national feed returned no records; skipping national import");
            return 0;
        }

        var existing = await _unitOfWork.CovidStatistics
            .ListAllAsync(cancellationToken)
            .ConfigureAwait(false);

        var byDate = existing.ToDictionary(s => s.Date);
        var utcNow = _dateTimeProvider.UtcNow;
        var changes = 0;

        foreach (var record in records)
        {
            var metrics = BuildMetrics(record);

            if (byDate.TryGetValue(record.Date, out var current))
            {
                if (!current.Metrics.Equals(metrics))
                {
                    current.UpdateMetrics(metrics, utcNow);
                    _unitOfWork.CovidStatistics.Update(current);
                    changes++;
                }
            }
            else
            {
                var statistic = CovidStatistic.Create(record.Date, metrics, utcNow);
                await _unitOfWork.CovidStatistics.AddAsync(statistic, cancellationToken).ConfigureAwait(false);
                changes++;
            }
        }

        return changes;
    }

    private async Task<int> ImportStateAsync(CancellationToken cancellationToken)
    {
        var records = await _dataProvider.GetStateDailyAsync(cancellationToken).ConfigureAwait(false);
        if (records.Count == 0)
        {
            _logger.LogWarning("MoH state feed returned no records; skipping state import");
            return 0;
        }

        var existing = await _unitOfWork.StateStatistics
            .ListAllAsync(cancellationToken)
            .ConfigureAwait(false);

        var byKey = existing.ToDictionary(s => (s.State.Code, s.Date));
        var utcNow = _dateTimeProvider.UtcNow;
        var changes = 0;

        foreach (var record in records)
        {
            if (!StateCode.TryParse(record.StateNameOrCode, out var state) || state is null)
            {
                _logger.LogDebug(
                    "Skipping unrecognised state '{State}' in MoH feed", record.StateNameOrCode);
                continue;
            }

            var metrics = BuildMetrics(record);

            if (byKey.TryGetValue((state.Code, record.Date), out var current))
            {
                if (!current.Metrics.Equals(metrics))
                {
                    current.UpdateMetrics(metrics, utcNow);
                    _unitOfWork.StateStatistics.Update(current);
                    changes++;
                }
            }
            else
            {
                var statistic = StateStatistic.Create(state, record.Date, metrics, utcNow);
                await _unitOfWork.StateStatistics.AddAsync(statistic, cancellationToken).ConfigureAwait(false);
                changes++;
            }
        }

        return changes;
    }

    private static CaseMetrics BuildMetrics(MohDailyRecord record) =>
        // The upstream feed occasionally publishes negative correction values;
        // clamp to zero so a single anomalous day cannot violate the domain
        // invariant and abort the whole batch.
        CaseMetrics.Create(
            Math.Max(0, record.NewCases),
            Math.Max(0, record.CumulativeCases),
            Math.Max(0, record.ActiveCases),
            Math.Max(0, record.Recovered),
            Math.Max(0, record.NewDeaths),
            Math.Max(0, record.CumulativeDeaths));
}
