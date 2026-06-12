using CovidAnalyticsPortal.Application.Common.Behaviours;
using FluentAssertions;
using FluentValidation;
using MediatR;

namespace CovidAnalyticsPortal.Tests.Unit.Application.Behaviours;

/// <summary>
/// Unit tests for <see cref="ValidationBehaviour{TRequest, TResponse}"/>,
/// covering the no-validators short-circuit, the pass-through on success, and
/// the aggregated <see cref="ValidationException"/> on failure.
/// </summary>
public sealed class ValidationBehaviourTests
{
    private sealed record Command(string Value);

    private sealed class PassingValidator : AbstractValidator<Command>
    {
        public PassingValidator() => RuleFor(c => c.Value).NotEmpty();
    }

    [Fact]
    public async Task Handle_WithNoValidators_CallsNext()
    {
        var sut = new ValidationBehaviour<Command, int>(Array.Empty<IValidator<Command>>());
        RequestHandlerDelegate<int> next = () => Task.FromResult(7);

        var result = await sut.Handle(new Command("ok"), next, CancellationToken.None);

        result.Should().Be(7);
    }

    [Fact]
    public async Task Handle_WhenValid_CallsNext()
    {
        var sut = new ValidationBehaviour<Command, int>(new[] { new PassingValidator() });
        RequestHandlerDelegate<int> next = () => Task.FromResult(11);

        var result = await sut.Handle(new Command("value"), next, CancellationToken.None);

        result.Should().Be(11);
    }

    [Fact]
    public async Task Handle_WhenInvalid_ThrowsValidationException()
    {
        var sut = new ValidationBehaviour<Command, int>(new[] { new PassingValidator() });
        RequestHandlerDelegate<int> next = () => Task.FromResult(0);

        var act = () => sut.Handle(new Command(string.Empty), next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
