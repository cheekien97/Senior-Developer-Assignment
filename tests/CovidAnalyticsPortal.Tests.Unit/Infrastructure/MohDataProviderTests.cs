using System.Net;
using CovidAnalyticsPortal.Infrastructure.ExternalServices.Moh;
using CovidAnalyticsPortal.Tests.Unit.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CovidAnalyticsPortal.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for <see cref="MohDataProvider"/>, verifying CSV parsing,
/// cumulative aggregation, national/state merging of cases and deaths, and the
/// in-memory caching behaviour — all without real network access.
/// </summary>
public sealed class MohDataProviderTests
{
    private readonly MohApiOptions _options = new();

    private MohDataProvider CreateProvider(
        StubHttpMessageHandler handler,
        IMemoryCache? cache = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/"),
        };

        return new MohDataProvider(
            httpClient,
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            Options.Create(_options),
            NullLogger<MohDataProvider>.Instance);
    }

    [Fact]
    public async Task GetNationalDailyAsync_ParsesAndAccumulatesCumulativeTotals()
    {
        var responses = new Dictionary<string, string>
        {
            [_options.NationalCasesPath] =
                "date,cases_new,cases_recovered,cases_active\n" +
                "2021-06-01,100,10,90\n" +
                "2021-06-02,200,20,180\n",
            [_options.NationalDeathsPath] =
                "date,deaths_new\n" +
                "2021-06-01,5\n" +
                "2021-06-02,7\n",
        };

        var provider = CreateProvider(new StubHttpMessageHandler(responses));

        var records = await provider.GetNationalDailyAsync();

        records.Should().HaveCount(2);
        records[0].NewCases.Should().Be(100);
        records[0].CumulativeCases.Should().Be(100);
        records[0].NewDeaths.Should().Be(5);
        records[1].CumulativeCases.Should().Be(300);
        records[1].CumulativeDeaths.Should().Be(12);
        records.All(r => r.StateNameOrCode is null).Should().BeTrue();
    }

    [Fact]
    public async Task GetStateDailyAsync_TracksCumulativeTotalsPerState()
    {
        var responses = new Dictionary<string, string>
        {
            [_options.StateCasesPath] =
                "date,state,cases_new,cases_recovered,cases_active\n" +
                "2021-06-01,Selangor,100,10,90\n" +
                "2021-06-01,Johor,50,5,45\n" +
                "2021-06-02,Selangor,150,15,135\n",
            [_options.StateDeathsPath] =
                "date,state,deaths_new\n" +
                "2021-06-01,Selangor,3\n" +
                "2021-06-02,Selangor,4\n",
        };

        var provider = CreateProvider(new StubHttpMessageHandler(responses));

        var records = await provider.GetStateDailyAsync();

        records.Should().HaveCount(3);

        var selangor = records.Where(r => r.StateNameOrCode == "Selangor").ToList();
        selangor.Should().HaveCount(2);
        selangor[1].CumulativeCases.Should().Be(250);
        selangor[1].CumulativeDeaths.Should().Be(7);

        var johor = records.Single(r => r.StateNameOrCode == "Johor");
        johor.CumulativeCases.Should().Be(50);
        johor.CumulativeDeaths.Should().Be(0);
    }

    [Fact]
    public async Task GetNationalDailyAsync_CachesResult_AndDoesNotRefetch()
    {
        var responses = new Dictionary<string, string>
        {
            [_options.NationalCasesPath] = "date,cases_new,cases_recovered,cases_active\n2021-06-01,100,10,90\n",
            [_options.NationalDeathsPath] = "date,deaths_new\n2021-06-01,5\n",
        };

        var handler = new StubHttpMessageHandler(responses);
        var provider = CreateProvider(handler);

        await provider.GetNationalDailyAsync();
        var requestsAfterFirst = handler.RequestCount;
        await provider.GetNationalDailyAsync();

        handler.RequestCount.Should().Be(requestsAfterFirst, "the second call should be served from cache");
    }

    [Fact]
    public async Task GetNationalDailyAsync_NonSuccessStatus_Throws()
    {
        var handler = new StubHttpMessageHandler(
            new Dictionary<string, string> { [_options.NationalCasesPath] = string.Empty },
            HttpStatusCode.InternalServerError);

        var provider = CreateProvider(handler);

        var act = () => provider.GetNationalDailyAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetNationalDailyAsync_SkipsRowsWithUnparseableDates()
    {
        var responses = new Dictionary<string, string>
        {
            [_options.NationalCasesPath] =
                "date,cases_new,cases_recovered,cases_active\n" +
                "not-a-date,100,10,90\n" +
                "2021-06-02,200,20,180\n",
            [_options.NationalDeathsPath] = "date,deaths_new\n2021-06-02,7\n",
        };

        var provider = CreateProvider(new StubHttpMessageHandler(responses));

        var records = await provider.GetNationalDailyAsync();

        records.Should().ContainSingle();
        records[0].Date.Should().Be(new DateOnly(2021, 6, 2));
    }
}
