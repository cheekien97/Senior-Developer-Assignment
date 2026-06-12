using CovidAnalyticsPortal.Application.Common.Interfaces;
using FluentValidation;

namespace CovidAnalyticsPortal.Application.Audit.Queries;

/// <summary>
/// Validates <see cref="GetAuditTrailQuery"/>, ensuring the optional date
/// window is coherent and not set in the future, and that the requested result
/// count stays within sensible bounds.
/// </summary>
public sealed class GetAuditTrailQueryValidator : AbstractValidator<GetAuditTrailQuery>
{
    private const int MaxAllowedResults = 500;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAuditTrailQueryValidator"/> class.
    /// </summary>
    /// <param name="dateTimeProvider">The clock used to validate against the current date.</param>
    public GetAuditTrailQueryValidator(IDateTimeProvider dateTimeProvider)
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

        RuleFor(q => q.MaxResults)
            .InclusiveBetween(1, MaxAllowedResults)
            .WithMessage($"The number of results must be between 1 and {MaxAllowedResults}.");
    }
}
