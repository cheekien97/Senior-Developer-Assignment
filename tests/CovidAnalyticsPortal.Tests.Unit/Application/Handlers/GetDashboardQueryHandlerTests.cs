using CovidAnalyticsPortal.Application.Common.Interfaces;
using CovidAnalyticsPortal.Application.Dashboard.Dtos;
using CovidAnalyticsPortal.Application.Dashboard.Queries;
using CovidAnalyticsPortal.Domain.ValueObjects;
using CovidAnalyticsPortal.Tests.Unit.TestHelpers;
using FluentAssertions;
using Moq;

namespace CovidAnalyticsPortal.Tests.Unit.Application.Handlers;

/// <summary>
/// Unit tests for <see cref="GetDashboardQueryHandler"/>, focusing on the
/// default reporting-window resolution and delegation to the dashboard service.
/// </summary>
public sealed class GetDashboardQueryHandlerTests
{
    private readonly Mock<IDashboardService> _service = new();
    private readonly FixedDateTimeProvider _clock = new(TestData.UtcNow);
    private readonly GetDashboardQueryHandler _sut;

    public GetDashboardQueryHandlerTests()
    {
        _sut = new GetDashboardQueryHandler(_service.Object, _clock);
    }

    private static DashboardDto Dto(DateRange period) => new()
    {
        AsOfDate = period.End,
        PeriodStart = period.Start,
        PeriodEnd = period.End,
    };

    [Fact]
    public async Task Handle_WithExplicitDates_UsesThoseDates()
    {
        DateRange? captured = null;
        _service
            .Setup(s => s.BuildDashboardAsync(It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .Callback<DateRange, CancellationToken>((range, _) => captured = range)
            .ReturnsAsync((DateRange range, CancellationToken _) => Dto(range));

        var query = new GetDashboardQuery(new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 10));

        await _sut.Handle(query, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Start.Should().Be(new DateOnly(2021, 6, 1));
        captured.End.Should().Be(new DateOnly(2021, 6, 10));
    }

    [Fact]
    public async Task Handle_WithNoDates_DefaultsToTrailing30DayWindowEndingToday()
    {
        DateRange? captured = null;
        _service
            .Setup(s => s.BuildDashboardAsync(It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .Callback<DateRange, CancellationToken>((range, _) => captured = range)
            .ReturnsAsync((DateRange range, CancellationToken _) => Dto(range));

        await _sut.Handle(new GetDashboardQuery(), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.End.Should().Be(_clock.Today);
        captured.Start.Should().Be(_clock.Today.AddDays(-29));
    }
}
