using System.Linq.Expressions;
using CovidAnalyticsPortal.Domain.Common;

namespace CovidAnalyticsPortal.Domain.Repositories;

/// <summary>
/// Generic, persistence-ignorant abstraction over an aggregate's storage.
/// Following the Dependency Inversion Principle, the domain owns this contract
/// while the infrastructure layer provides the concrete implementation. All
/// operations are asynchronous and accept a <see cref="CancellationToken"/> to
/// support responsive, cancellable I/O.
/// </summary>
/// <typeparam name="TEntity">The aggregate root type managed by the repository.</typeparam>
public interface IRepository<TEntity>
    where TEntity : Entity
{
    /// <summary>
    /// Retrieves an entity by its identity.
    /// </summary>
    /// <param name="id">The identity of the entity to retrieve.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The matching entity, or <c>null</c> if none exists.</returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities of the managed type.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A read-only list of all entities.</returns>
    Task<IReadOnlyList<TEntity>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities that satisfy the supplied predicate.
    /// </summary>
    /// <param name="predicate">The filter expression.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A read-only list of matching entities.</returns>
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether any entity satisfies the supplied predicate.
    /// </summary>
    /// <param name="predicate">The condition to test.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns><c>true</c> if at least one entity matches; otherwise, <c>false</c>.</returns>
    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the repository. The change is not persisted until
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a range of new entities to the repository. The changes are not
    /// persisted until <see cref="IUnitOfWork.SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an existing entity as modified. The change is not persisted until
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    void Update(TEntity entity);

    /// <summary>
    /// Marks an entity for removal. The change is not persisted until
    /// <see cref="IUnitOfWork.SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    void Remove(TEntity entity);
}
