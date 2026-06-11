using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CovidAnalyticsPortal.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="StateStatistic"/>. Converts the
/// <see cref="StateCode"/> value object to and from its canonical string code
/// and maps the <c>CaseMetrics</c> value object as an owned type.
/// </summary>
public sealed class StateStatisticConfiguration
    : IEntityTypeConfiguration<StateStatistic>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StateStatistic> builder)
    {
        builder.ToTable("StateStatistics");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.State)
            .HasConversion(
                state => state.Code,
                code => StateCode.Create(code))
            .HasColumnName("StateCode")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(s => s.Date)
            .IsRequired();

        builder.HasIndex(s => new { s.State, s.Date })
            .IsUnique()
            .HasDatabaseName("IX_StateStatistics_State_Date");

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
