# Data Flow Diagram

How COVID-19 data moves from the upstream Ministry of Health feed into the local store, and how it is read back by the analytics features. This reflects the actual background service, importer, provider, persistence, and read services.

## Ingestion (write path)

```mermaid
sequenceDiagram
    participant Host as API start-up
    participant DB as Database
    participant T as SyncBackgroundService
    participant I as CovidDataImporter
    participant P as MohDataProvider
    participant Cache as IMemoryCache
    participant MoH as MoH datasets
    participant UoW as IUnitOfWork
    participant L as Serilog

    Host->>DB: MigrateDatabaseAsync apply EF migrations
    Note over T: after InitialDelaySeconds, then every IntervalHours
    T->>I: ImportAsync
    I->>P: GetNationalDailyAsync and GetStateDailyAsync
    P->>Cache: check cache
    alt cache miss
        P->>MoH: GET cases and deaths CSV with retry, timeout, circuit breaker
        MoH-->>P: CSV snapshot
        P->>Cache: store normalised records
    end
    P-->>I: list of MohDailyRecord
    I->>UoW: upsert CovidStatistic and StateStatistic, idempotent
    UoW->>DB: SaveChangesAsync, atomic
    I->>L: log import outcome
    Note over T,L: a failed run is caught and logged, retried next interval
```

## Querying (read path)

```mermaid
flowchart LR
    subgraph Read
        Q["Query handler<br/>(Dashboard / Statistics / Trends)"]
        S["DashboardService / AnalyticsService"]
        R["EfRepository&lt;T&gt; via IUnitOfWork<br/>AsNoTracking + predicate"]
    end
    DB[("CovidStatistic · StateStatistic<br/>SQLite / SQL Server")]
    Dto["DTO<br/>DashboardDto · StateStatisticDto · TrendDto"]
    API["API controller → JSON"]
    Web["Web (ICovidApiClient) → ViewModel → Razor + Chart.js"]

    Q --> S --> R --> DB
    DB --> R --> S
    S --> Dto --> API --> Web
```

## Data shaping

- **Upstream → domain:** `MohDataProvider` normalises raw CSV rows; `CovidDataImporter` maps them into domain value objects — `CaseMetrics` (six non-negative counters), `StateCode` (validated ISO 3166-2:MY), and `DateOnly` — and into `CovidStatistic` / `StateStatistic` aggregates.
- **Domain → DTO:** read services project entities into flat, serialisation-friendly DTOs. `AnalyticsService` additionally builds a `TrendRecord` in the domain to compute change, percentage change, and direction for `TrendDto`.
- **Idempotency:** ingestion upserts by natural key (date, and state for state data), so repeated runs converge rather than duplicate.
- **Freshness:** the local store lags the source by at most `CovidDataSync:IntervalHours` (default 12h).
