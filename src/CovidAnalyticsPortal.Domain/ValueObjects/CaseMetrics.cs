using CovidAnalyticsPortal.Domain.Common;
using CovidAnalyticsPortal.Domain.Exceptions;

namespace CovidAnalyticsPortal.Domain.ValueObjects;

/// <summary>
/// An immutable snapshot of COVID-19 case figures for a single day. Grouping
/// the related counters into one value object keeps the owning entities focused
/// on identity and lifecycle, and centralises the invariant that no counter can
/// be negative.
/// </summary>
public sealed class CaseMetrics : ValueObject
{
    /// <summary>Gets the number of new confirmed cases recorded on the day.</summary>
    public long NewCases { get; }

    /// <summary>Gets the running total of confirmed cases up to and including the day.</summary>
    public long CumulativeCases { get; }

    /// <summary>Gets the number of currently active (unresolved) cases.</summary>
    public long ActiveCases { get; }

    /// <summary>Gets the number of recovered cases recorded on the day.</summary>
    public long Recovered { get; }

    /// <summary>Gets the number of new deaths recorded on the day.</summary>
    public long NewDeaths { get; }

    /// <summary>Gets the running total of deaths up to and including the day.</summary>
    public long CumulativeDeaths { get; }

    private CaseMetrics(
        long newCases,
        long cumulativeCases,
        long activeCases,
        long recovered,
        long newDeaths,
        long cumulativeDeaths)
    {
        NewCases = newCases;
        CumulativeCases = cumulativeCases;
        ActiveCases = activeCases;
        Recovered = recovered;
        NewDeaths = newDeaths;
        CumulativeDeaths = cumulativeDeaths;
    }

    /// <summary>
    /// Gets a <see cref="CaseMetrics"/> instance with all counters set to zero.
    /// </summary>
    public static CaseMetrics Zero { get; } = new(0, 0, 0, 0, 0, 0);

    /// <summary>
    /// Creates a validated <see cref="CaseMetrics"/> instance.
    /// </summary>
    /// <param name="newCases">New confirmed cases for the day.</param>
    /// <param name="cumulativeCases">Cumulative confirmed cases.</param>
    /// <param name="activeCases">Currently active cases.</param>
    /// <param name="recovered">Recovered cases for the day.</param>
    /// <param name="newDeaths">New deaths for the day.</param>
    /// <param name="cumulativeDeaths">Cumulative deaths.</param>
    /// <returns>A valid <see cref="CaseMetrics"/> instance.</returns>
    /// <exception cref="DomainException">Thrown when any supplied counter is negative.</exception>
    public static CaseMetrics Create(
        long newCases,
        long cumulativeCases,
        long activeCases,
        long recovered,
        long newDeaths,
        long cumulativeDeaths)
    {
        DomainException.ThrowIf(newCases < 0, "New cases cannot be negative.");
        DomainException.ThrowIf(cumulativeCases < 0, "Cumulative cases cannot be negative.");
        DomainException.ThrowIf(activeCases < 0, "Active cases cannot be negative.");
        DomainException.ThrowIf(recovered < 0, "Recovered cases cannot be negative.");
        DomainException.ThrowIf(newDeaths < 0, "New deaths cannot be negative.");
        DomainException.ThrowIf(cumulativeDeaths < 0, "Cumulative deaths cannot be negative.");

        return new CaseMetrics(
            newCases,
            cumulativeCases,
            activeCases,
            recovered,
            newDeaths,
            cumulativeDeaths);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return NewCases;
        yield return CumulativeCases;
        yield return ActiveCases;
        yield return Recovered;
        yield return NewDeaths;
        yield return CumulativeDeaths;
    }
}
