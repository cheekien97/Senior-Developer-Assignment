using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CovidAnalyticsPortal.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="TrendRecord"/>. Converts the optional
/// <see cref="StateCode"/> to a nullable string and maps the
/// <c>Period</c> <see cref="DateRange"/> value object as an owned type.
/// </summary>
public sealed class TrendRecordConfiguration
    : IEntityTypeConfiguration<TrendRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TrendRecord> builder)
    {
        builder.ToTable("TrendRecords");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.State)
            .HasConversion(
                state => state == null ? null : state.Code,
                code => code == null ? null : StateCode.Create(code))
            .HasColumnName("StateCode")
            .HasMaxLength(3);

        builder.Property(t => t.Metric)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.OwnsOne(t => t.Period, period =>
        {
            period.Property(p => p.Start).HasColumnName("PeriodStart").IsRequired();
            period.Property(p => p.End).HasColumnName("PeriodEnd").IsRequired();
        });

        builder.Navigation(t => t.Period).IsRequired();

        builder.Property(t => t.StartValue).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.EndValue).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.ChangeValue).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.ChangePercentage).HasPrecision(18, 2).IsRequired();

        builder.Property(t => t.Direction)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.LastModifiedAtUtc);
    }
}
