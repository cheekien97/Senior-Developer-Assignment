using System.Net;
using System.Net.Http.Json;
using CovidAnalyticsPortal.Application.Statistics.Dtos;
using CovidAnalyticsPortal.Application.Trends.Dtos;
using FluentAssertions;

namespace CovidAnalyticsPortal.Tests.Integration;

/// <summary>
/// End-to-end tests for the analytics endpoints (statistics and trends).
/// </summary>
public sealed class AnalyticsEndpointsTests : IClassFixture<CovidApiFactory>
{
    private readonly HttpClient _client;

    public AnalyticsEndpointsTests(CovidApiFactory factory)
    {
        factory.EnsureSeeded();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStatistics_WithoutStateFilter_ReturnsAllStates()
    {
        var response = await _client.GetAsync(
            $"/api/v1.0/analytics/statistics?from={CovidApiFactory.EarliestDate:yyyy-MM-dd}&to={CovidApiFactory.LatestDate:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dtos = await response.Content.ReadFromJsonAsync<List<StateStatisticDto>>();
        dtos.Should().NotBeNullOrEmpty();
        dtos!.Select(d => d.StateCode).Distinct().Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task GetStatistics_WithStateFilter_ReturnsOnlyThatState()
    {
        var response = await _client.GetAsync(
            $"/api/v1.0/analytics/statistics?from={CovidApiFactory.EarliestDate:yyyy-MM-dd}&to={CovidApiFactory.LatestDate:yyyy-MM-dd}&state=SGR");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dtos = await response.Content.ReadFromJsonAsync<List<StateStatisticDto>>();
        dtos.Should().NotBeNullOrEmpty();
        dtos!.Should().OnlyContain(d => d.StateCode == "SGR");
    }

    [Fact]
    public async Task GetStatistics_WithUnknownState_Returns400()
    {
        var response = await _client.GetAsync(
            "/api/v1.0/analytics/statistics?from=2021-06-01&to=2021-06-30&state=ZZZ");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTrends_National_ReturnsTrendWithSeries()
    {
        var response = await _client.GetAsync(
            $"/api/v1.0/analytics/trends?metric=Cases&from={CovidApiFactory.EarliestDate:yyyy-MM-dd}&to={CovidApiFactory.LatestDate:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<TrendDto>();
        dto.Should().NotBeNull();
        dto!.Metric.Should().Be("Cases");
        dto.StateName.Should().BeNull();
        dto.Points.Should().NotBeEmpty();
        dto.Direction.Should().Be("Increasing");
    }

    [Fact]
    public async Task GetTrends_State_ReturnsNamedTrend()
    {
        var response = await _client.GetAsync(
            $"/api/v1.0/analytics/trends?metric=Deaths&from={CovidApiFactory.EarliestDate:yyyy-MM-dd}&to={CovidApiFactory.LatestDate:yyyy-MM-dd}&state=SGR");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<TrendDto>();
        dto!.StateName.Should().Be("Selangor");
        dto.Points.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTrends_WithUndefinedMetric_Returns400()
    {
        var response = await _client.GetAsync(
            "/api/v1.0/analytics/trends?metric=NotAMetric&from=2021-06-01&to=2021-06-30");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
