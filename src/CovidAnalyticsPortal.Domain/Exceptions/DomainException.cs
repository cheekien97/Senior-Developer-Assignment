namespace CovidAnalyticsPortal.Domain.Exceptions;

/// <summary>
/// Represents an error that occurs when a domain invariant or business rule is
/// violated. Throwing this exception keeps invalid state out of the domain
/// model and provides a single, catchable type for the outer layers to map to
/// an appropriate response (e.g. HTTP 400).
/// </summary>
public sealed class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class
    /// with a descriptive message.
    /// </summary>
    /// <param name="message">A message describing the violated rule.</param>
    public DomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class
    /// with a descriptive message and an inner exception.
    /// </summary>
    /// <param name="message">A message describing the violated rule.</param>
    /// <param name="innerException">The underlying cause of this exception.</param>
    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Throws a <see cref="DomainException"/> when the supplied condition is
    /// <c>true</c>. A convenience guard for enforcing invariants.
    /// </summary>
    /// <param name="condition">The condition that, when <c>true</c>, indicates a rule violation.</param>
    /// <param name="message">The message to attach to the thrown exception.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="condition"/> is <c>true</c>.</exception>
    public static void ThrowIf(bool condition, string message)
    {
        if (condition)
        {
            throw new DomainException(message);
        }
    }
}
