using CovidAnalyticsPortal.Application.Trends.Queries;
using CovidAnalyticsPortal.Domain.Enums;
using CovidAnalyticsPortal.Tests.Unit.TestHelpers;
using FluentValidation.TestHelper;

namespace CovidAnalyticsPortal.Tests.Unit.Validators;

/// <summary>
/// Unit tests for <see cref="GetTrendAnalysisQueryValidator"/>.
/// </summary>
public sealed class GetTrendAnalysisQueryValidatorTests
{
    private static readonly DateOnly Today = new(2021, 6, 30);
    private readonly GetTrendAnalysisQueryValidator _validator =
        new(new FixedDateTimeProvider(Today.ToDateTime(TimeOnly.MinValue)));

    [Fact]
    public void ValidQuery_IsValid()
    {
        var query = new GetTrendAnalysisQuery(MetricType.Cases, new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 10), "SGR");
        _validator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UndefinedMetric_IsInvalid()
    {
        var query = new GetTrendAnalysisQuery((MetricType)999, new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 10));
        _validator.TestValidate(query).ShouldHaveValidationErrorFor(q => q.Metric);
    }

    [Fact]
    public void EndBeforeStart_IsInvalid()
    {
        var query = new GetTrendAnalysisQuery(MetricType.Deaths, new DateOnly(2021, 6, 10), new DateOnly(2021, 6, 1));
        _validator.TestValidate(query).ShouldHaveValidationErrorFor(q => q.To);
    }

    [Fact]
    public void UnknownState_IsInvalid()
    {
        var query = new GetTrendAnalysisQuery(MetricType.Cases, new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 10), "XYZ");
        _validator.TestValidate(query).ShouldHaveValidationErrorFor(q => q.State);
    }
}
