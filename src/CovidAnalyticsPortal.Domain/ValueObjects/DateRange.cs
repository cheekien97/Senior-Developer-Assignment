using CovidAnalyticsPortal.Domain.Common;
using CovidAnalyticsPortal.Domain.Exceptions;

namespace CovidAnalyticsPortal.Domain.ValueObjects;

/// <summary>
/// An immutable, inclusive range of calendar dates. Encapsulates the invariant
/// that the end date can never precede the start date, so any value of this
/// type is guaranteed to be valid.
/// </summary>
public sealed class DateRange : ValueObject
{
    /// <summary>
    /// Gets the inclusive start date of the range.
    /// </summary>
    public DateOnly Start { get; }

    /// <summary>
    /// Gets the inclusive end date of the range.
    /// </summary>
    public DateOnly End { get; }

    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Gets the number of days contained in the range, inclusive of both
    /// endpoints.
    /// </summary>
    public int LengthInDays => (End.DayNumber - Start.DayNumber) + 1;

    /// <summary>
    /// Creates a validated <see cref="DateRange"/>.
    /// </summary>
    /// <param name="start">The inclusive start date.</param>
    /// <param name="end">The inclusive end date.</param>
    /// <returns>A valid <see cref="DateRange"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="end"/> precedes <paramref name="start"/>.</exception>
    public static DateRange Create(DateOnly start, DateOnly end)
    {
        DomainException.ThrowIf(
            end < start,
            $"The end date '{end:yyyy-MM-dd}' cannot precede the start date '{start:yyyy-MM-dd}'.");

        return new DateRange(start, end);
    }

    /// <summary>
    /// Determines whether the supplied date falls within the range (inclusive).
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <returns><c>true</c> if the date is within the range; otherwise, <c>false</c>.</returns>
    public bool Contains(DateOnly date) => date >= Start && date <= End;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }

    /// <inheritdoc />
    public override string ToString() => $"{Start:yyyy-MM-dd} to {End:yyyy-MM-dd}";
}
