# ADR-005: COVID Data Integration

- **Status:** Accepted
- **Date:** 2026-06
- **Deciders:** Engineering

## Context

The portal's data originates from the Ministry of Health Malaysia open datasets (CSV files in `github.com/MoH-Malaysia/covid19-public`). That feed is an external dependency: it can be slow, rate-limited, or briefly unavailable, and its data are daily snapshots spanning 2020–2022. We needed a strategy that:

- serves fast, filterable analytics without calling the upstream feed on every request;
- keeps the portal available and responsive when the feed is degraded;
- provisions data with no manual import step on a clean machine; and
- isolates the external contract from the domain.

## Decision

Ingest the MoH datasets into a **local persistence store** that acts as a read model, fed by a resilient typed client and a scheduled background importer.

- **Abstraction:** `IMohDataProvider` (Application) defines the contract; `MohDataProvider` (Infrastructure) implements it as a typed `HttpClient` that fetches and normalises the national/state cases and deaths CSVs into a common record shape, caching results in `IMemoryCache`.
- **Resilience:** the client is registered with `AddStandardResilienceHandler` (retry, attempt/total-request timeouts, circuit breaker), tuned from `MohApiOptions` (`TimeoutSeconds`, `RetryCount`). The base URL is fixed via validated options (`ValidateDataAnnotations` + `ValidateOnStart`), preventing SSRF.
- **Scheduling:** `CovidDataSyncBackgroundService` (a hosted `BackgroundService`) runs after `InitialDelaySeconds` and then every `IntervalHours`; it can be disabled via `CovidDataSync:Enabled`. Failures in a run are caught and logged, and retried on the next interval — the host never crashes.
- **Import:** `CovidDataImporter` performs an idempotent upsert into `CovidStatistic` / `StateStatistic` through the Unit of Work, mapping raw records into domain value objects (`CaseMetrics`, `StateCode`, `DateOnly`).
- **Schema:** EF Core migrations are applied at API start-up via `MigrateDatabaseAsync` for zero-touch setup; `AppDbContextFactory` supports design-time tooling.
- **Provider flexibility:** persistence targets SQLite by default and SQL Server in production via `Database:Provider`, with retry-on-failure enabled for SQL Server.

## Consequences

**Positive**

- Request latency is decoupled from the upstream feed; aggregation and trend computation happen locally.
- The portal stays available during transient upstream outages; ingestion self-heals on the next interval.
- Zero manual setup — schema and data appear automatically on first run.
- The external CSV contract is confined to Infrastructure behind `IMohDataProvider`; the domain never sees it.

**Negative / costs**

- The local store can lag the source between sync runs; data freshness is bounded by `IntervalHours`.
- Start-up migration and per-instance background ingestion need attention under scale-out (see below).
- Ingestion volume (full national/state history) makes the first run the heaviest.

## Alternatives Considered

- **Query the MoH feed on every request (no local store).** Simplest data model, but slow, fragile to upstream limits/outages, and unable to compute server-side aggregates efficiently. Rejected.
- **On-demand/manual sync only.** Avoids a background service but breaks the zero-touch goal and risks an empty dashboard on first run. Rejected; an idempotent scheduled importer was chosen instead.
- **Apply migrations as a separate deploy step from day one.** Safer for multi-instance production, but adds friction to local development. We chose start-up migration for zero-touch dev and documented gating it for multi-instance production (README §13).
- **Single-instance leader for ingestion now.** Deferred: the background service currently runs per host; leader election or a dedicated worker is the documented path when scaling out.
