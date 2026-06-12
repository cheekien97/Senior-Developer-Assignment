using CovidAnalyticsPortal.Application.Common.Interfaces;

namespace CovidAnalyticsPortal.Tests.Unit.TestHelpers;

/// <summary>
/// A simple, fixed <see cref="ICurrentContext"/> for tests so the audit service
/// can be exercised without an HTTP request context.
/// </summary>
internal sealed class StubCurrentContext : ICurrentContext
{
    public StubCurrentContext(
        string actor = "TestUser",
        string correlationId = "test-correlation-id",
        string? ipAddress = "127.0.0.1")
    {
        Actor = actor;
        CorrelationId = correlationId;
        IpAddress = ipAddress;
    }

    public string Actor { get; }

    public string CorrelationId { get; }

    public string? IpAddress { get; }
}
