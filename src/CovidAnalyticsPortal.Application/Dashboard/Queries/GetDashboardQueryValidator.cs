using CovidAnalyticsPortal.Application.Common.Interfaces;
using FluentValidation;

namespace CovidAnalyticsPortal.Application.Dashboard.Queries;

/// <summary>
/// Validates <see cref="GetDashboardQuery"/>, ensuring the optional date window
/// is coherent and not set in the future.
/// </summary>
public sealed class GetDashboardQueryValidator : AbstractValidator<GetDashboardQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetDashboardQueryValidator"/> class.
    /// </summary>
    /// <param name="dateTimeProvider">The clock used to validate against the current date.</param>
    public GetDashboardQueryValidator(IDateTimeProvider dateTimeProvider)
    {
        When(q => q.From.HasValue && q.To.HasValue, () =>
        {
            RuleFor(q => q.To!.Value)
                .GreaterThanOrEqualTo(q => q.From!.Value)
                .WithMessage("The end date must be on or after the start date.");
        });

        RuleFor(q => q.To!.Value)
            .LessThanOrEqualTo(_ => dateTimeProvider.Today)
            .When(q => q.To.HasValue)
            .WithMessage("The end date cannot be in the future.");
    }
}
