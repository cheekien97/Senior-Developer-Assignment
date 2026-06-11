using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Domain.Repositories;
using CovidAnalyticsPortal.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging;

namespace CovidAnalyticsPortal.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/>. Owns a single
/// <see cref="AppDbContext"/> instance and exposes one repository per aggregate,
/// all sharing that context so their changes are committed together by
/// <see cref="SaveChangesAsync"/>. Repositories are created lazily on first
/// access.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;

    private IRepository<CovidStatistic>? _covidStatistics;
    private IRepository<StateStatistic>? _stateStatistics;
    private IRepository<TrendRecord>? _trendRecords;
    private IRepository<AuditTrail>? _auditTrails;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="context">The shared database context.</param>
    /// <param name="logger">The logger used to record persistence outcomes.</param>
    public UnitOfWork(AppDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public IRepository<CovidStatistic> CovidStatistics =>
        _covidStatistics ??= new EfRepository<CovidStatistic>(_context);

    /// <inheritdoc />
    public IRepository<StateStatistic> StateStatistics =>
        _stateStatistics ??= new EfRepository<StateStatistic>(_context);

    /// <inheritdoc />
    public IRepository<TrendRecord> TrendRecords =>
        _trendRecords ??= new EfRepository<TrendRecord>(_context);

    /// <inheritdoc />
    public IRepository<AuditTrail> AuditTrails =>
        _auditTrails ??= new EfRepository<AuditTrail>(_context);

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var affected = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Unit of work committed {AffectedRecords} change(s) to the data store",
            affected);

        return affected;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // The AppDbContext is owned and disposed by the dependency-injection
        // container (registered via AddDbContext as a scoped service) and is
        // shared with other scoped consumers within the same request. The unit
        // of work therefore deliberately does not dispose it here, avoiding a
        // double-dispose of a resource it does not own.
    }
}
