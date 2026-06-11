using CovidAnalyticsPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CovidAnalyticsPortal.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="CovidStatistic"/>. Maps the
/// <c>CaseMetrics</c> value object as an owned type so its counters are stored
/// in the same table while remaining encapsulated in the domain.
/// </summary>
public sealed class CovidStatisticConfiguration
    : IEntityTypeConfiguration<CovidStatistic>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CovidStatistic> builder)
    {
        builder.ToTable("CovidStatistics");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Date)
            .IsRequired();

        builder.HasIndex(s => s.Date)
            .IsUnique()
            .HasDatabaseName("IX_CovidStatistics_Date");

        builder.Property(s => s.CreatedAtUtc)
            .IsRequired();

        builder.Property(s => s.LastModifiedAtUtc);

        builder.OwnsOne(s => s.Metrics, metrics =>
        {
            metrics.Property(m => m.NewCases).HasColumnName("NewCases").IsRequired();
            metrics.Property(m => m.CumulativeCases).HasColumnName("CumulativeCases").IsRequired();
            metrics.Property(m => m.ActiveCases).HasColumnName("ActiveCases").IsRequired();
            metrics.Property(m => m.Recovered).HasColumnName("Recovered").IsRequired();
            metrics.Property(m => m.NewDeaths).HasColumnName("NewDeaths").IsRequired();
            metrics.Property(m => m.CumulativeDeaths).HasColumnName("CumulativeDeaths").IsRequired();
        });

        builder.Navigation(s => s.Metrics).IsRequired();
    }
}
