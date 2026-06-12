using CovidAnalyticsPortal.Application.Common.Behaviours;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace CovidAnalyticsPortal.Tests.Unit.Application.Behaviours;

/// <summary>
/// Unit tests for <see cref="LoggingBehaviour{TRequest, TResponse}"/>, verifying
/// it returns the handler's response on success and rethrows on failure.
/// </summary>
public sealed class LoggingBehaviourTests
{
    private readonly LoggingBehaviour<string, int> _sut =
        new(NullLogger<LoggingBehaviour<string, int>>.Instance);

    [Fact]
    public async Task Handle_OnSuccess_ReturnsHandlerResponse()
    {
        RequestHandlerDelegate<int> next = () => Task.FromResult(42);

        var result = await _sut.Handle("request", next, CancellationToken.None);

        result.Should().Be(42);
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_RethrowsException()
    {
        RequestHandlerDelegate<int> next = () => throw new InvalidOperationException("handler failed");

        var act = () => _sut.Handle("request", next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("handler failed");
    }
}
