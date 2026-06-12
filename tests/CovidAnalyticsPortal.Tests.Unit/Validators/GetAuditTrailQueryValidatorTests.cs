using CovidAnalyticsPortal.Application.Audit.Queries;
using CovidAnalyticsPortal.Domain.Enums;
using CovidAnalyticsPortal.Tests.Unit.TestHelpers;
using FluentValidation.TestHelper;

namespace CovidAnalyticsPortal.Tests.Unit.Validators;

/// <summary>
/// Unit tests for <see cref="GetAuditTrailQueryValidator"/>.
/// </summary>
public sealed class GetAuditTrailQueryValidatorTests
{
    private readonly GetAuditTrailQueryValidator _validator =
        new(new FixedDateTimeProvider(TestData.UtcNow));

    [Fact]
    public void Valid_WhenWithinBounds()
    {
        var query = new GetAuditTrailQuery(
            From: new DateOnly(2021, 6, 1), To: new DateOnly(2021, 6, 10), Action: AuditAction.ViewDashboard, MaxResults: 50);

        _validator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Invalid_WhenEndBeforeStart()
    {
        var query = new GetAuditTrailQuery(
            From: new DateOnly(2021, 6, 10), To: new DateOnly(2021, 6, 1));

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(q => q.To!.Value);
    }

    [Fact]
    public void Invalid_WhenEndInFuture()
    {
        var query = new GetAuditTrailQuery(To: DateOnly.FromDateTime(TestData.UtcNow).AddDays(1));

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(q => q.To!.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public void Invalid_WhenMaxResultsOutOfRange(int maxResults)
    {
        var query = new GetAuditTrailQuery(MaxResults: maxResults);

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(q => q.MaxResults);
    }
}
