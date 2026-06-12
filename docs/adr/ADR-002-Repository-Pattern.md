# ADR-002: Repository Pattern

- **Status:** Accepted
- **Date:** 2026-06
- **Deciders:** Engineering

## Context

The Application layer needs to read and write domain aggregates without depending on EF Core, so that the dependency rule (ADR-001) holds and the application services remain unit-testable. We needed a persistence contract that:

- lives in an inner layer (so Application/Domain do not reference EF Core);
- supports the read-centric query patterns the portal needs (filter by date range and optional state); and
- can be substituted with a test double.

The alternative was to inject `AppDbContext` / `DbSet<T>` directly into application services.

## Decision

Define a generic **repository** abstraction in the Domain and implement it in Infrastructure.

- `IRepository<TEntity> where TEntity : Entity` (Domain) exposes `GetByIdAsync`, `ListAllAsync`, `FindAsync(Expression<Func<TEntity,bool>>)`, `ExistsAsync`, `AddAsync`, `AddRangeAsync`, `Update`, and `Remove`.
- `EfRepository<TEntity>` (Infrastructure) wraps a `DbSet<TEntity>`:
  - reads use `AsNoTracking()`;
  - `FindAsync` takes a LINQ predicate that EF translates to parameterised SQL;
  - write methods only **stage** changes — commit is deferred to the Unit of Work (ADR-003).
- The open generic `IRepository<>` → `EfRepository<>` is registered in DI for any direct consumers; application services normally reach repositories through `IUnitOfWork`.

## Consequences

**Positive**

- EF Core stays entirely within Infrastructure; inner layers depend only on the abstraction.
- Application services (`DashboardService`, `AnalyticsService`) are unit-tested with a mocked `IRepository<T>`.
- A single, consistent data-access surface; `AsNoTracking` reads are efficient for the portal's read paths.
- Expression-based `FindAsync` keeps filtering in the database, not in memory.

**Negative / costs**

- Some consider a repository over EF Core redundant since `DbContext` is already a Unit of Work + repository. We accept this trade-off for boundary cleanliness and testability.
- The generic contract is deliberately minimal; specialised queries are composed via predicates rather than bespoke repository methods.

## Alternatives Considered

- **Inject `AppDbContext` directly into services.** Simplest, but leaks EF Core into the Application layer, breaks the dependency rule, and makes pure unit testing impractical. Rejected.
- **One concrete repository interface per aggregate** (e.g. `ICovidStatisticRepository`). More explicit, but adds many near-identical types; the generic `IRepository<T>` plus predicate-based `FindAsync` covers current needs with less code. Can be introduced later if a query becomes complex enough to warrant a dedicated method.
- **Specification pattern.** Powerful for composable queries, but more machinery than the current, modest query set justifies.
