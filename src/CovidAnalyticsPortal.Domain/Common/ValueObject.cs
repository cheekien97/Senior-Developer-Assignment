namespace CovidAnalyticsPortal.Domain.Common;

/// <summary>
/// Base class for value objects in the Domain-Driven Design sense. A value
/// object has no conceptual identity and is considered equal to another value
/// object when all of its constituent components are equal. Value objects are
/// immutable.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Supplies the ordered set of components that participate in equality
    /// comparison. Derived types must yield every field that contributes to
    /// the value object's identity.
    /// </summary>
    /// <returns>The sequence of equality components.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = default(HashCode);

        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Determines whether two value objects are equal.
    /// </summary>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        return left is not null && left.Equals(right);
    }

    /// <summary>
    /// Determines whether two value objects are not equal.
    /// </summary>
    public static bool operator !=(ValueObject? left, ValueObject? right) =>
        !(left == right);
}
