using CovidAnalyticsPortal.Application.Common.Interfaces;

namespace CovidAnalyticsPortal.Tests.Unit.TestHelpers;

/// <summary>
/// A deterministic <see cref="IDateTimeProvider"/> for tests, returning a fixed
/// instant so time-dependent logic is reproducible.
/// </summary>
internal sealed class FixedDateTimeProvider : IDateTimeProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixedDateTimeProvider"/> class.
    /// </summary>
    /// <param name="utcNow">The fixed UTC instant to return.</param>
    public FixedDateTimeProvider(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    /// <inheritdoc />
    public DateTime UtcNow { get; }

    /// <inheritdoc />
    public DateOnly Today => DateOnly.FromDateTime(UtcNow);
}
