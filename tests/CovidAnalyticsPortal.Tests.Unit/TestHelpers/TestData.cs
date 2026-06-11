using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Domain.ValueObjects;

namespace CovidAnalyticsPortal.Tests.Unit.TestHelpers;

/// <summary>
/// Central factory for building domain entities and value objects used across
/// the unit-test suite, keeping the individual tests concise and consistent.
/// </summary>
internal static class TestData
{
    /// <summary>A stable UTC instant used for audit stamping in tests.</summary>
    public static readonly DateTime UtcNow = new(2021, 6, 30, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Builds a <see cref="CaseMetrics"/> instance with sensible, overridable defaults.
    /// </summary>
    public static CaseMetrics Metrics(
        long newCases = 100,
        long cumulativeCases = 1_000,
        long activeCases = 200,
        long recovered = 50,
        long newDeaths = 5,
        long cumulativeDeaths = 80) =>
        CaseMetrics.Create(newCases, cumulativeCases, activeCases, recovered, newDeaths, cumulativeDeaths);

    /// <summary>
    /// Builds a national <see cref="CovidStatistic"/> for a given day.
    /// </summary>
    public static CovidStatistic National(DateOnly date, CaseMetrics? metrics = null) =>
        CovidStatistic.Create(date, metrics ?? Metrics(), UtcNow);

    /// <summary>
    /// Builds a <see cref="StateStatistic"/> for a given state code and day.
    /// </summary>
    public static StateStatistic State(string stateCode, DateOnly date, CaseMetrics? metrics = null) =>
        StateStatistic.Create(StateCode.Create(stateCode), date, metrics ?? Metrics(), UtcNow);
}
