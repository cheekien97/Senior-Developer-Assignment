using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Domain.Enums;
using CovidAnalyticsPortal.Domain.Exceptions;
using FluentAssertions;

namespace CovidAnalyticsPortal.Tests.Unit.Domain;

/// <summary>
/// Unit tests for the <see cref="AuditTrail"/> aggregate, covering creation,
/// trimming, and the required-field invariants.
/// </summary>
public sealed class AuditTrailTests
{
    private static readonly DateTime Now = new(2021, 6, 30, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidData_PopulatesAndTrimsFields()
    {
        var entry = AuditTrail.Create(
            AuditAction.ApplyFilter,
            "  Applied a filter  ",
            "  Alice  ",
            "  corr-1  ",
            Now,
            entityName: "Statistics",
            parameters: "state=SGR",
            ipAddress: "10.0.0.1");

        entry.Action.Should().Be(AuditAction.ApplyFilter);
        entry.Description.Should().Be("Applied a filter");
        entry.Actor.Should().Be("Alice");
        entry.CorrelationId.Should().Be("corr-1");
        entry.EntityName.Should().Be("Statistics");
        entry.Parameters.Should().Be("state=SGR");
        entry.IpAddress.Should().Be("10.0.0.1");
        entry.TimestampUtc.Should().Be(Now);
        entry.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("", "actor", "corr")]
    [InlineData("desc", "", "corr")]
    [InlineData("desc", "actor", "")]
    public void Create_WithMissingRequiredField_Throws(string description, string actor, string correlationId)
    {
        var act = () => AuditTrail.Create(AuditAction.SystemError, description, actor, correlationId, Now);

        act.Should().Throw<DomainException>();
    }
}
