using CovidAnalyticsPortal.Infrastructure.Persistence;
using CovidAnalyticsPortal.Tests.Unit.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CovidAnalyticsPortal.Tests.Unit.Repositories;

/// <summary>
/// Unit tests for <see cref="UnitOfWork"/>, verifying that the repositories
/// share a single context and that changes commit atomically.
/// </summary>
public sealed class UnitOfWorkTests : IDisposable
{
    private readonly SqliteContextHarness _harness = new();

    [Fact]
    public async Task SaveChangesAsync_CommitsChangesFromMultipleRepositories()
    {
        await using var context = _harness.CreateContext();
        var unitOfWork = new UnitOfWork(context, NullLogger<UnitOfWork>.Instance);

        await unitOfWork.CovidStatistics.AddAsync(TestData.National(new DateOnly(2021, 6, 1)));
        await unitOfWork.StateStatistics.AddAsync(TestData.State("SGR", new DateOnly(2021, 6, 1)));

        var affected = await unitOfWork.SaveChangesAsync();

        affected.Should().Be(2);
    }

    [Fact]
    public void Repositories_AreCachedAcrossAccesses()
    {
        using var context = _harness.CreateContext();
        var unitOfWork = new UnitOfWork(context, NullLogger<UnitOfWork>.Instance);

        unitOfWork.CovidStatistics.Should().BeSameAs(unitOfWork.CovidStatistics);
        unitOfWork.StateStatistics.Should().BeSameAs(unitOfWork.StateStatistics);
        unitOfWork.TrendRecords.Should().BeSameAs(unitOfWork.TrendRecords);
        unitOfWork.AuditTrails.Should().BeSameAs(unitOfWork.AuditTrails);
    }

    [Fact]
    public async Task ChangesAreVisible_AfterCommit_InNewContext()
    {
        await using (var context = _harness.CreateContext())
        {
            var unitOfWork = new UnitOfWork(context, NullLogger<UnitOfWork>.Instance);
            await unitOfWork.CovidStatistics.AddAsync(TestData.National(new DateOnly(2021, 6, 2)));
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = _harness.CreateContext())
        {
            var unitOfWork = new UnitOfWork(context, NullLogger<UnitOfWork>.Instance);
            var all = await unitOfWork.CovidStatistics.ListAllAsync();
            all.Should().ContainSingle();
        }
    }

    public void Dispose() => _harness.Dispose();
}
