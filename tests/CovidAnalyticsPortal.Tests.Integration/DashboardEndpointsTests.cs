using System.Net;
using System.Net.Http.Json;
using CovidAnalyticsPortal.Application.Dashboard.Dtos;
using FluentAssertions;

namespace CovidAnalyticsPortal.Tests.Integration;

/// <summary>
/// End-to-end tests for the dashboard endpoint, exercising the full HTTP
/// pipeline against a seeded in-memory database.
/// </summary>
public sealed class DashboardEndpointsTests : IClassFixture<CovidApiFactory>
{
    private readonly CovidApiFactory _factory;
    private readonly HttpClient _client;

    public DashboardEndpointsTests(CovidApiFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDashboard_WithSeededRange_Returns200AndAggregates()
    {
        var response = await _client.GetAsync(
            $"/api/v1.0/dashboard?from={CovidApiFactory.EarliestDate:yyyy-MM-dd}&to={CovidApiFactory.LatestDate:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<DashboardDto>();
        dto.Should().NotBeNull();
        dto!.AsOfDate.Should().Be(CovidApiFactory.LatestDate);
        dto.TotalCases.Should().BeGreaterThan(0);
        dto.StateBreakdown.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDashboard_ReturnsJsonContentType()
    {
        var response = await _client.GetAsync(
            $"/api/v1.0/dashboard?from={CovidApiFactory.EarliestDate:yyyy-MM-dd}&to={CovidApiFactory.LatestDate:yyyy-MM-dd}");

        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetDashboard_WithInvalidRange_Returns400ProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1.0/dashboard?from=2021-06-30&to=2021-06-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("errors");
    }

    [Fact]
    public async Task GetDashboard_IncludesCorrelationIdHeader()
    {
        var response = await _client.GetAsync(
            $"/api/v1.0/dashboard?from={CovidApiFactory.EarliestDate:yyyy-MM-dd}&to={CovidApiFactory.LatestDate:yyyy-MM-dd}");

        response.Headers.Contains("X-Correlation-ID").Should().BeTrue();
    }
}
