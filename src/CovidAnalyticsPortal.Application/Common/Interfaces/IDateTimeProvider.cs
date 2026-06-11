namespace CovidAnalyticsPortal.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the system clock. Injecting time rather than calling
/// <see cref="DateTime.UtcNow"/> directly keeps the application layer
/// deterministic and unit-testable, in line with the Dependency Inversion
/// Principle.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current Coordinated Universal Time (UTC).
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Gets the current UTC calendar date.
    /// </summary>
    DateOnly Today { get; }
}
