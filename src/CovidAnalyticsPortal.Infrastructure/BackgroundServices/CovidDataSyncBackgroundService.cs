using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CovidAnalyticsPortal.Infrastructure.BackgroundServices;

/// <summary>
/// Long-running background service that periodically synchronises the local
/// data store with the Ministry of Health open-data feed. It runs an initial
/// import shortly after start-up — so the dashboard is populated without any
/// manual setup — and then repeats on a configurable interval. Upstream
/// failures are caught and logged so a transient outage never stops the host
/// or the recurring schedule.
/// </summary>
public sealed class CovidDataSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CovidDataSyncOptions _options;
    private readonly ILogger<CovidDataSyncBackgroundService> _logger;

    /// <summary>
    /// Initialises a new instance of the <see cref="CovidDataSyncBackgroundService"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory used to create a DI scope per import run.</param>
    /// <param name="options">The bound synchronisation options.</param>
    /// <param name="logger">The logger.</param>
    public CovidDataSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<CovidDataSyncOptions> options,
        ILogger<CovidDataSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("COVID data synchronisation is disabled; background service will not run");
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(_options.InitialDelaySeconds), stoppingToken)
                .ConfigureAwait(false);

            await RunSafelyAsync(stoppingToken).ConfigureAwait(false);

            using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.IntervalHours));
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await RunSafelyAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown; nothing to do.
        }
    }

    private async Task RunSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting scheduled COVID data synchronisation");

            await using var scope = _scopeFactory.CreateAsyncScope();
            var importer = scope.ServiceProvider.GetRequiredService<CovidDataImporter>();

            await importer.ImportAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Ingestion must never crash the host or stop the recurring schedule;
            // a transient upstream failure is logged and retried next interval.
            _logger.LogError(
                exception,
                "COVID data synchronisation failed; will retry on the next scheduled run");
        }
    }
}
