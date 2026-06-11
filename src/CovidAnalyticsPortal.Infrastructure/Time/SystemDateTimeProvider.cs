using CovidAnalyticsPortal.Application.Common.Interfaces;

namespace CovidAnalyticsPortal.Infrastructure.Time;

/// <summary>
/// System-clock implementation of <see cref="IDateTimeProvider"/>. This is the
/// single place in the codebase permitted to read the ambient clock, which
/// keeps the rest of the application deterministic and testable.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;

    /// <inheritdoc />
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
