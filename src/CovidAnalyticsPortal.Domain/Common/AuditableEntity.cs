namespace CovidAnalyticsPortal.Domain.Common;

/// <summary>
/// Base class for entities that require audit timestamps. Captures when the
/// entity was first created and last modified, expressed in UTC to avoid
/// time-zone ambiguity.
/// </summary>
public abstract class AuditableEntity : Entity
{
    /// <summary>
    /// Gets the UTC timestamp at which the entity was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; protected set; }

    /// <summary>
    /// Gets the UTC timestamp at which the entity was last modified, or
    /// <c>null</c> if it has never been modified since creation.
    /// </summary>
    public DateTime? LastModifiedAtUtc { get; protected set; }

    /// <summary>
    /// Initializes a new auditable entity with a freshly generated identity.
    /// </summary>
    protected AuditableEntity()
    {
    }

    /// <summary>
    /// Initializes a new auditable entity with the supplied identity.
    /// </summary>
    /// <param name="id">The pre-existing identity of the entity.</param>
    protected AuditableEntity(Guid id)
        : base(id)
    {
    }

    /// <summary>
    /// Stamps the creation time of the entity. Should be called once by a
    /// factory method when the entity is first created.
    /// </summary>
    /// <param name="utcNow">The current UTC time.</param>
    protected void MarkCreated(DateTime utcNow)
    {
        CreatedAtUtc = utcNow;
        LastModifiedAtUtc = null;
    }

    /// <summary>
    /// Stamps the last-modified time of the entity. Should be called whenever
    /// the entity's state is mutated.
    /// </summary>
    /// <param name="utcNow">The current UTC time.</param>
    protected void MarkModified(DateTime utcNow)
    {
        LastModifiedAtUtc = utcNow;
    }
}
