using CovidAnalyticsPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CovidAnalyticsPortal.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="AuditTrail"/>. The audit table is treated as
/// append-only at the application level; indexes are added on the columns most
/// commonly used to query the audit trail (timestamp, action, correlation id).
/// </summary>
public sealed class AuditTrailConfiguration
    : IEntityTypeConfiguration<AuditTrail>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuditTrail> builder)
    {
        builder.ToTable("AuditTrails");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(a => a.Description)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(a => a.Actor)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(a => a.CorrelationId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.EntityName)
            .HasMaxLength(256);

        builder.Property(a => a.Parameters)
            .HasMaxLength(2048);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(64);

        builder.Property(a => a.TimestampUtc)
            .IsRequired();

        builder.HasIndex(a => a.TimestampUtc)
            .HasDatabaseName("IX_AuditTrails_TimestampUtc");

        builder.HasIndex(a => a.Action)
            .HasDatabaseName("IX_AuditTrails_Action");

        builder.HasIndex(a => a.CorrelationId)
            .HasDatabaseName("IX_AuditTrails_CorrelationId");
    }
}
