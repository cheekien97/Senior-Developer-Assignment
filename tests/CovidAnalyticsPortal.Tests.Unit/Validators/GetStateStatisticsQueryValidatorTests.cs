using CovidAnalyticsPortal.Application.Statistics.Queries;
using CovidAnalyticsPortal.Tests.Unit.TestHelpers;
using FluentValidation.TestHelper;

namespace CovidAnalyticsPortal.Tests.Unit.Validators;

/// <summary>
/// Unit tests for <see cref="GetStateStatisticsQueryValidator"/>.
/// </summary>
public sealed class GetStateStatisticsQueryValidatorTests
{
    private static readonly DateOnly Today = new(2021, 6, 30);
    private readonly GetStateStatisticsQueryValidator _validator =
        new(new FixedDateTimeProvider(Today.ToDateTime(TimeOnly.MinValue)));

    [Fact]
    public void ValidRangeNoState_IsValid()
    {
        var query = new GetStateStatisticsQuery(new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 10));
        _validator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidRangeWithKnownState_IsValid()
    {
        var query = new GetStateStatisticsQuery(new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 10), "SGR");
        _validator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EndBeforeStart_IsInvalid()
    {
        var query = new GetStateStatisticsQuery(new DateOnly(2021, 6, 10), new DateOnly(2021, 6, 1));
        _validator.TestValidate(query).ShouldHaveValidationErrorFor(q => q.To);
    }

    [Fact]
    public void EndInFuture_IsInvalid()
    {
        var query = new GetStateStatisticsQuery(new DateOnly(2021, 6, 1), Today.AddDays(5));
        _validator.TestValidate(query).ShouldHaveValidationErrorFor(q => q.To);
    }

    [Theory]
    [InlineData("ZZZ")]
    [InlineData("NotAState")]
    public void UnknownState_IsInvalid(string state)
    {
        var query = new GetStateStatisticsQuery(new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 10), state);
        _validator.TestValidate(query).ShouldHaveValidationErrorFor(q => q.State);
    }
}
