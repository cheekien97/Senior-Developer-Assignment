using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Infrastructure.Persistence.Repositories;
using CovidAnalyticsPortal.Tests.Unit.TestHelpers;
using FluentAssertions;

namespace CovidAnalyticsPortal.Tests.Unit.Repositories;

/// <summary>
/// Unit tests for the generic <see cref="EfRepository{TEntity}"/>, run against a
/// real in-memory SQLite database so the EF Core query translation and entity
/// configuration are genuinely exercised.
/// </summary>
public sealed class EfRepositoryTests : IDisposable
{
    private readonly SqliteContextHarness _harness = new();

    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsEntity()
    {
        var entity = TestData.National(new DateOnly(2021, 6, 1));

        await using (var context = _harness.CreateContext())
        {
            var repository = new EfRepository<CovidStatistic>(context);
            await repository.AddAsync(entity);
            await context.SaveChangesAsync();
        }

        await using (var context = _harness.CreateContext())
        {
            var repository = new EfRepository<CovidStatistic>(context);
            var loaded = await repository.GetByIdAsync(entity.Id);
            loaded.Should().NotBeNull();
            loaded!.Date.Should().Be(new DateOnly(2021, 6, 1));
            loaded.Metrics.NewCases.Should().Be(entity.Metrics.NewCases);
        }
    }

    [Fact]
    public async Task AddRangeAsync_PersistsAllEntities()
    {
        var entities = new[]
        {
            TestData.State("SGR", new DateOnly(2021, 6, 1)),
            TestData.State("JHR", new DateOnly(2021, 6, 1)),
        };

        await using (var context = _harness.CreateContext())
        {
            var repository = new EfRepository<StateStatistic>(context);
            await repository.AddRangeAsync(entities);
            await context.SaveChangesAsync();
        }

        await using (var context = _harness.CreateContext())
        {
            var repository = new EfRepository<StateStatistic>(context);
            var all = await repository.ListAllAsync();
            all.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task FindAsync_AppliesPredicate()
    {
        await SeedNationalAsync(
            TestData.National(new DateOnly(2021, 6, 1)),
            TestData.National(new DateOnly(2021, 6, 10)),
            TestData.National(new DateOnly(2021, 6, 20)));

        await using var context = _harness.CreateContext();
        var repository = new EfRepository<CovidStatistic>(context);

        var matches = await repository.FindAsync(s => s.Date >= new DateOnly(2021, 6, 10));

        matches.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrueWhenMatchPresent()
    {
        await SeedNationalAsync(TestData.National(new DateOnly(2021, 6, 5)));

        await using var context = _harness.CreateContext();
        var repository = new EfRepository<CovidStatistic>(context);

        (await repository.ExistsAsync(s => s.Date == new DateOnly(2021, 6, 5))).Should().BeTrue();
        (await repository.ExistsAsync(s => s.Date == new DateOnly(1999, 1, 1))).Should().BeFalse();
    }

    [Fact]
    public async Task Update_PersistsModifiedMetrics()
    {
        var entity = TestData.National(new DateOnly(2021, 6, 1), TestData.Metrics(newCases: 10));
        await SeedNationalAsync(entity);

        await using (var context = _harness.CreateContext())
        {
            var repository = new EfRepository<CovidStatistic>(context);
            var loaded = await repository.GetByIdAsync(entity.Id);
            loaded!.UpdateMetrics(TestData.Metrics(newCases: 999), TestData.UtcNow);
            repository.Update(loaded);
            await context.SaveChangesAsync();
        }

        await using (var context = _harness.CreateContext())
        {
            var repository = new EfRepository<CovidStatistic>(context);
            var reloaded = await repository.GetByIdAsync(entity.Id);
            reloaded!.Metrics.NewCases.Should().Be(999);
        }
    }

    [Fact]
    public async Task Remove_DeletesEntity()
    {
        var entity = TestData.National(new DateOnly(2021, 6, 1));
        await SeedNationalAsync(entity);

        await using (var context = _harness.CreateContext())
        {
            var repository = new EfRepository<CovidStatistic>(context);
            var loaded = await repository.GetByIdAsync(entity.Id);
            repository.Remove(loaded!);
            await context.SaveChangesAsync();
        }

        await using (var context = _harness.CreateContext())
        {
            var repository = new EfRepository<CovidStatistic>(context);
            (await repository.ListAllAsync()).Should().BeEmpty();
        }
    }

    private async Task SeedNationalAsync(params CovidStatistic[] entities)
    {
        await using var context = _harness.CreateContext();
        var repository = new EfRepository<CovidStatistic>(context);
        await repository.AddRangeAsync(entities);
        await context.SaveChangesAsync();
    }

    public void Dispose() => _harness.Dispose();
}
