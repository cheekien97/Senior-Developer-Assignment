namespace CovidAnalyticsPortal.Domain.Common;

/// <summary>
/// Base class for all domain entities. Provides a strongly-typed identity and
/// identity-based equality semantics, in line with Domain-Driven Design (DDD)
/// where an entity is defined by its identity rather than its attribute values.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Initializes a new entity with a freshly generated identity.
    /// </summary>
    protected Entity()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Initializes a new entity with the supplied identity. Intended for
    /// rehydration scenarios (e.g. mapping from persistence).
    /// </summary>
    /// <param name="id">The pre-existing identity of the entity.</param>
    protected Entity(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// Determines whether the specified entity is equal to the current entity
    /// based on runtime type and identity.
    /// </summary>
    /// <param name="other">The entity to compare with the current entity.</param>
    /// <returns><c>true</c> if the entities share the same type and identity; otherwise, <c>false</c>.</returns>
    public bool Equals(Entity? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return Id != Guid.Empty && Id.Equals(other.Id);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Entity);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <summary>
    /// Determines whether two entities are equal.
    /// </summary>
    public static bool operator ==(Entity? left, Entity? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines whether two entities are not equal.
    /// </summary>
    public static bool operator !=(Entity? left, Entity? right) =>
        !Equals(left, right);
}
