using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Domain.Exceptions;
using FluentAssertions;

namespace CovidAnalyticsPortal.Tests.Unit.Domain;

/// <summary>
/// Unit tests for <see cref="DomainException"/> and the identity-based equality
/// of the <c>Entity</c> base class.
/// </summary>
public sealed class DomainPrimitivesTests
{
    [Fact]
    public void ThrowIf_WhenConditionTrue_Throws()
    {
        var act = () => DomainException.ThrowIf(true, "rule violated");

        act.Should().Throw<DomainException>().WithMessage("rule violated");
    }

    [Fact]
    public void ThrowIf_WhenConditionFalse_DoesNotThrow()
    {
        var act = () => DomainException.ThrowIf(false, "rule violated");

        act.Should().NotThrow();
    }

    [Fact]
    public void Entities_WithDistinctIdentities_AreNotEqual()
    {
        var date = new DateOnly(2021, 6, 1);
        var metrics = CovidAnalyticsPortal.Domain.ValueObjects.CaseMetrics.Zero;
        var now = DateTime.UtcNow;

        var a = CovidStatistic.Create(date, metrics, now);
        var b = CovidStatistic.Create(date, metrics, now);
        var sameReference = a;

        a.Equals(sameReference).Should().BeTrue();
        a.Equals(b).Should().BeFalse("they have distinct generated identities");
        (a == sameReference).Should().BeTrue();
        (a != b).Should().BeTrue();
        a.GetHashCode().Should().Be(sameReference.GetHashCode());
    }

    [Fact]
    public void Entity_Equals_Null_ReturnsFalse()
    {
        var entity = CovidStatistic.Create(new DateOnly(2021, 6, 1), CovidAnalyticsPortal.Domain.ValueObjects.CaseMetrics.Zero, DateTime.UtcNow);

        entity.Equals(null).Should().BeFalse();
        entity.Equals((object?)null).Should().BeFalse();
    }
}
