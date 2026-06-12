using CovidAnalyticsPortal.Domain.Entities;
using CovidAnalyticsPortal.Domain.Enums;
using CovidAnalyticsPortal.Domain.Repositories;
using CovidAnalyticsPortal.Infrastructure.Audit;
using CovidAnalyticsPortal.Infrastructure.Persistence;
using CovidAnalyticsPortal.Tests.Unit.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CovidAnalyticsPortal.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for <see cref="AuditService"/>, verifying that entries are
/// persisted with ambient context and that auditing failures are swallowed so
/// they never break the primary request flow.
/// </summary>
public sealed class AuditServiceTests : IDisposable
{
    private readonly SqliteContextHarness _harness = new();
    private readonly FixedDateTimeProvider _clock = new(TestData.UtcNow);

    [Fact]
    public async Task RecordAsync_PersistsEntry_WithAmbientContext()
    {
        await using var context = _harness.CreateContext();
        var unitOfWork = new UnitOfWork(context, NullLogger<UnitOfWork>.Instance);
        var currentContext = new StubCurrentContext(actor: "Alice", correlationId: "corr-1", ipAddress: "10.0.0.1");
        var sut = new AuditService(unitOfWork, currentContext, _clock, NullLogger<AuditService>.Instance);

        await sut.RecordAsync(AuditAction.ViewDashboard, "Viewed dashboard", "Dashboard", "from=2021-06-01");

        await using var verifyContext = _harness.CreateContext();
        var verifyUow = new UnitOfWork(verifyContext, NullLogger<UnitOfWork>.Instance);
        var entries = await verifyUow.AuditTrails.ListAllAsync();

        var entry = entries.Should().ContainSingle().Subject;
        entry.Action.Should().Be(AuditAction.ViewDashboard);
        entry.Actor.Should().Be("Alice");
        entry.CorrelationId.Should().Be("corr-1");
        entry.IpAddress.Should().Be("10.0.0.1");
        entry.TimestampUtc.Should().Be(TestData.UtcNow);
    }

    [Fact]
    public async Task RecordAsync_WhenPersistenceFails_DoesNotThrow()
    {
        var failingUow = new Mock<IUnitOfWork>();
        var failingRepo = new Mock<IRepository<AuditTrail>>();
        failingRepo
            .Setup(r => r.AddAsync(It.IsAny<AuditTrail>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));
        failingUow.SetupGet(u => u.AuditTrails).Returns(failingRepo.Object);

        var sut = new AuditService(
            failingUow.Object,
            new StubCurrentContext(),
            _clock,
            NullLogger<AuditService>.Instance);

        var act = () => sut.RecordAsync(AuditAction.SystemError, "boom");

        await act.Should().NotThrowAsync();
    }

    public void Dispose() => _harness.Dispose();
}
