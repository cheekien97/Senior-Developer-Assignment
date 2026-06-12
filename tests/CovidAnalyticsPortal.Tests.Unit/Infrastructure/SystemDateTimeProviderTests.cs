using CovidAnalyticsPortal.Infrastructure.Time;
using FluentAssertions;

namespace CovidAnalyticsPortal.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for <see cref="SystemDateTimeProvider"/>.
/// </summary>
public sealed class SystemDateTimeProviderTests
{
    [Fact]
    public void UtcNow_ReturnsCurrentUtcInstant()
    {
        var sut = new SystemDateTimeProvider();

        var before = DateTime.UtcNow;
        var value = sut.UtcNow;
        var after = DateTime.UtcNow;

        value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Today_MatchesUtcDate()
    {
        var sut = new SystemDateTimeProvider();

        sut.Today.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
