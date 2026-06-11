using System.Linq.Expressions;
using CovidAnalyticsPortal.Domain.Common;
using CovidAnalyticsPortal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CovidAnalyticsPortal.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic EF Core implementation of <see cref="IRepository{TEntity}"/>. Wraps a
/// <see cref="DbSet{TEntity}"/> and translates the persistence-ignorant domain
/// contract into asynchronous EF Core operations. Read queries use
/// <c>AsNoTracking</c> for efficiency, while write operations defer persistence
/// to the owning <see cref="UnitOfWork"/>.
/// </summary>
/// <typeparam name="TEntity">The aggregate root type managed by the repository.</typeparam>
public class EfRepository<TEntity> : IRepository<TEntity>
    where TEntity : Entity
{
    /// <summary>
    /// Gets the database context shared with the owning unit of work.
    /// </summary>
    protected AppDbContext Context { get; }

    /// <summary>
    /// Gets the entity set managed by this repository.
    /// </summary>
    protected DbSet<TEntity> Set { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EfRepository{TEntity}"/> class.
    /// </summary>
    /// <param name="context">The database context to operate against.</param>
    public EfRepository(AppDbContext context)
    {
        Context = context;
        Set = context.Set<TEntity>();
    }

    /// <inheritdoc />
    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Set.FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(predicate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .AnyAsync(predicate, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default) =>
        await Set.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public void Update(TEntity entity) => Set.Update(entity);

    /// <inheritdoc />
    public void Remove(TEntity entity) => Set.Remove(entity);
}
