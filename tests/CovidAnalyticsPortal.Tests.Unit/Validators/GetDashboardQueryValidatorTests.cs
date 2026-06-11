using CovidAnalyticsPortal.Application.Dashboard.Queries;
using CovidAnalyticsPortal.Tests.Unit.TestHelpers;
using FluentValidation.TestHelper;

namespace CovidAnalyticsPortal.Tests.Unit.Validators;

/// <summary>
/// Unit tests for <see cref="GetDashboardQueryValidator"/>.
/// </summary>
public sealed class GetDashboardQueryValidatorTests
{
    private static readonly DateOnly Today = new(2021, 6, 30);
    private readonly GetDashboardQueryValidator _validator =
        new(new FixedDateTimeProvider(Today.ToDateTime(TimeOnly.MinValue)));

    [Fact]
    public void NoDates_IsValid()
    {
        var result = _validator.TestValidate(new GetDashboardQuery());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CoherentPastRange_IsValid()
    {
        var query = new GetDashboardQuery(new DateOnly(2021, 6, 1), new DateOnly(2021, 6, 30));
        _validator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EndBeforeStart_IsInvalid()
    {
        var query = new GetDashboardQuery(new DateOnly(2021, 6, 30), new DateOnly(2021, 6, 1));
        _validator.TestValidate(query).ShouldHaveValidationErrorFor(q => q.To!.Value);
    }

    [Fact]
    public void EndInFuture_IsInvalid()
    {
        var query = new GetDashboardQuery(new DateOnly(2021, 6, 1), Today.AddDays(1));
        _validator.TestValidate(query).ShouldHaveValidationErrorFor(q => q.To!.Value);
    }
}
