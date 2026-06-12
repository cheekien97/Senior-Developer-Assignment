# Architecture Decision Records

This directory holds the Architecture Decision Records (ADRs) for the COVID-19 Analytics Portal. Each record captures one significant decision in a standard format: **Status, Context, Decision, Consequences, Alternatives Considered**.

| ADR | Decision | Status |
|-----|----------|--------|
| [ADR-001](ADR-001-Clean-Architecture.md) | Adopt Clean Architecture (4 rings / 5 projects, inward dependency rule) | Accepted |
| [ADR-002](ADR-002-Repository-Pattern.md) | Generic repository abstraction in Domain, EF implementation in Infrastructure | Accepted |
| [ADR-003](ADR-003-UnitOfWork.md) | Unit of Work as the single commit boundary over shared `DbContext` | Accepted |
| [ADR-004](ADR-004-Logging-and-Observability.md) | Serilog operational logging + correlation IDs, separate persisted audit trail | Accepted |
| [ADR-005](ADR-005-COVID-Data-Integration.md) | Ingest MoH datasets into a local read store via a resilient scheduled importer | Accepted |

See also [`../Architecture.md`](../Architecture.md) and the [diagrams](../diagrams/).
