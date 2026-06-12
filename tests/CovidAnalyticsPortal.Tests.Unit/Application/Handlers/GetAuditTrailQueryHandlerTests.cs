using CovidAnalyticsPortal.Application.Audit.Queries;
using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Domain.Enums;
using CovidAnalyticsPortal.Infrastructure.Persistence;
using CovidAnalyticsPortal.Tests.Unit.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CovidAnalyticsPortal.Tests.Unit.Application.Handlers;

/// <summary>
/// Unit tests for <see cref="GetAuditTrailQueryHandler"/>, exercising date and
/// action filtering, most-recent-first ordering, the result cap, and DTO
/// projection against a real in-memory store.
/// </summary>
public sealed class GetAuditTrailQueryHandlerTests : IDisposable
{
    private readonly SqliteContextHarness _harness = new();

    private async Task SeedAsync(params AuditTrail[] entries)
    {
        await using var context = _harness.CreateContext();
        var uow = new UnitOfWork(context, NullLogger<UnitOfWork>.Instance);
        await uow.AuditTrails.AddRangeAsync(entries);
        await uow.SaveChangesAsync();
    }

    private static AuditTrail Entry(AuditAction action, DateTime timestampUtc) =>
        AuditTrail.Create(action, $"{action} event", "TestUser", "corr", timestampUtc);

    private GetAuditTrailQueryHandler CreateHandler(AppDbContext context) =>
        new(new UnitOfWork(context, NullLogger<UnitOfWork>.Instance));

    [Fact]
    public async Task Handle_OrdersMostRecentFirst_AndProjectsToDto()
    {
        await SeedAsync(
            Entry(AuditAction.ViewDashboard, new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc)),
            Entry(AuditAction.ViewTrends, new DateTime(2021, 6, 3, 0, 0, 0, DateTimeKind.Utc)),
            Entry(AuditAction.ViewStatistics, new DateTime(2021, 6, 2, 0, 0, 0, DateTimeKind.Utc)));

        await using var context = _harness.CreateContext();
        var result = await CreateHandler(context).Handle(new GetAuditTrailQuery(), CancellationToken.None);

        result.Should().HaveCount(3);
        result[0].Action.Should().Be(AuditAction.ViewTrends.ToString());
        result[2].Action.Should().Be(AuditAction.ViewDashboard.ToString());
    }

    [Fact]
    public async Task Handle_FiltersByAction()
    {
        await SeedAsync(
            Entry(AuditAction.ViewDashboard, new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc)),
            Entry(AuditAction.ViewTrends, new DateTime(2021, 6, 2, 0, 0, 0, DateTimeKind.Utc)));

        await using var context = _harness.CreateContext();
        var query = new GetAuditTrailQuery(Action: AuditAction.ViewTrends);
        var result = await CreateHandler(context).Handle(query, CancellationToken.None);

        result.Should().ContainSingle()
            .Which.Action.Should().Be(AuditAction.ViewTrends.ToString());
    }

    [Fact]
    public async Task Handle_FiltersByDateRange_Inclusive()
    {
        await SeedAsync(
            Entry(AuditAction.ViewDashboard, new DateTime(2021, 5, 31, 0, 0, 0, DateTimeKind.Utc)),
            Entry(AuditAction.ViewTrends, new DateTime(2021, 6, 2, 12, 0, 0, DateTimeKind.Utc)),
            Entry(AuditAction.ViewStatistics, new DateTime(2021, 6, 5, 0, 0, 0, DateTimeKind.Utc)));

        await using var context = _harness.CreateContext();
        var query = new GetAuditTrailQuery(
            From: new DateOnly(2021, 6, 1), To: new DateOnly(2021, 6, 2));
        var result = await CreateHandler(context).Handle(query, CancellationToken.None);

        result.Should().ContainSingle()
            .Which.Action.Should().Be(AuditAction.ViewTrends.ToString());
    }

    [Fact]
    public async Task Handle_RespectsMaxResults()
    {
        var entries = Enumerable.Range(0, 5)
            .Select(i => Entry(AuditAction.ApplyFilter, new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(i)))
            .ToArray();
        await SeedAsync(entries);

        await using var context = _harness.CreateContext();
        var result = await CreateHandler(context).Handle(new GetAuditTrailQuery(MaxResults: 2), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    public void Dispose() => _harness.Dispose();
}
