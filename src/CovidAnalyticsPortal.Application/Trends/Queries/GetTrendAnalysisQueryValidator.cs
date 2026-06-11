using CovidAnalyticsPortal.Application.Common.Interfaces;
using CovidAnalyticsPortal.Domain.Enums;
using CovidAnalyticsPortal.Domain.ValueObjects;
using FluentValidation;

namespace CovidAnalyticsPortal.Application.Trends.Queries;

/// <summary>
/// Validates <see cref="GetTrendAnalysisQuery"/>, ensuring the metric is a
/// defined value, the date range is coherent, and any supplied state filter is
/// a recognised Malaysian state.
/// </summary>
public sealed class GetTrendAnalysisQueryValidator
    : AbstractValidator<GetTrendAnalysisQuery>
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="GetTrendAnalysisQueryValidator"/> class.
    /// </summary>
    /// <param name="dateTimeProvider">The clock used to validate against the current date.</param>
    public GetTrendAnalysisQueryValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(q => q.Metric)
            .IsInEnum()
            .WithMessage("The requested metric is not supported.");

        RuleFor(q => q.To)
            .GreaterThanOrEqualTo(q => q.From)
            .WithMessage("The end date must be on or after the start date.");

        RuleFor(q => q.To)
            .LessThanOrEqualTo(_ => dateTimeProvider.Today)
            .WithMessage("The end date cannot be in the future.");

        RuleFor(q => q.State)
            .Must(BeAValidState)
            .When(q => !string.IsNullOrWhiteSpace(q.State))
            .WithMessage("The supplied value is not a recognised Malaysian state or federal territory.");
    }

    private static bool BeAValidState(string? state) =>
        StateCode.TryParse(state, out _);
}
