# ADR-003: Unit of Work

- **Status:** Accepted
- **Date:** 2026-06
- **Deciders:** Engineering

## Context

Several operations touch more than one aggregate or write multiple rows that must succeed or fail together:

- `AuditService` appends an `AuditTrail` and persists it;
- `CovidDataImporter` upserts many national and state statistics in one synchronisation run.

With the repository pattern (ADR-002) deferring persistence, we needed a single, explicit **commit boundary** so related changes are committed atomically and share one `DbContext` / change tracker. We also needed to control the lifetime/ownership of the `DbContext` to avoid double-dispose issues, given it is a scoped DI service shared within a request.

## Decision

Introduce a **Unit of Work** abstraction in the Domain and implement it in Infrastructure.

- `IUnitOfWork : IDisposable` (Domain) exposes one repository per aggregate — `CovidStatistics`, `StateStatistics`, `TrendRecords`, `AuditTrails` — plus `Task<int> SaveChangesAsync(ct)`.
- `UnitOfWork` (Infrastructure) owns a single `AppDbContext`. Each repository property is created **lazily** over that same context, so all repositories share one change tracker and transaction.
- `SaveChangesAsync` commits all staged changes atomically and logs the affected-row count.
- `Dispose` intentionally does **not** dispose the `AppDbContext`: the context is a scoped service owned by the DI container and shared with other scoped consumers in the same request; disposing it here would double-dispose a resource the UoW does not own.
- `IUnitOfWork` → `UnitOfWork` is registered scoped; it is the primary persistence entry point for application services.

## Consequences

**Positive**

- A single, intention-revealing commit point; related writes are atomic (e.g. importer upserts, audit writes).
- All repositories within a scope share one `DbContext`, so the change tracker behaves consistently and transactions are coherent.
- Clear ownership semantics avoid `ObjectDisposedException`/double-dispose bugs.
- Services depend on one abstraction (`IUnitOfWork`) that is easy to mock in unit tests.

**Negative / costs**

- An extra abstraction layered over EF Core's own change tracking (which is itself a Unit of Work). Accepted for explicitness and for keeping EF Core out of inner layers.
- Callers must remember to call `SaveChangesAsync`; staged changes are not auto-committed.

## Alternatives Considered

- **Call `SaveChanges` inside each repository write.** Simple, but loses atomicity across multiple writes and issues unnecessary round-trips. Rejected.
- **Use `DbContext` directly as the Unit of Work.** Works technically, but exposes EF Core to inner layers and abandons the persistence-agnostic contract from ADR-002. Rejected.
- **Ambient transaction (`TransactionScope`).** Heavier and provider-sensitive; unnecessary given a single shared `DbContext` already provides the transaction boundary.
