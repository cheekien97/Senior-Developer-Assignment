using CovidAnalyticsPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CovidAnalyticsPortal.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for the COVID-19 Analytics Portal.
/// Exposes the aggregate roots as queryable sets and applies all entity
/// configurations discovered in this assembly. The context is the technical
/// boundary between the domain model and the relational store.
/// </summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">The options configured by the host (provider, connection string, etc.).</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the set of national, country-level daily statistics.
    /// </summary>
    public DbSet<CovidStatistic> CovidStatistics => Set<CovidStatistic>();

    /// <summary>
    /// Gets the set of state-level daily statistics.
    /// </summary>
    public DbSet<StateStatistic> StateStatistics => Set<StateStatistic>();

    /// <summary>
    /// Gets the set of computed trend records.
    /// </summary>
    public DbSet<TrendRecord> TrendRecords => Set<TrendRecord>();

    /// <summary>
    /// Gets the set of audit trail entries.
    /// </summary>
    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Discover and apply every IEntityTypeConfiguration in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
