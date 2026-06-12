# ADR-004: Logging and Observability

- **Status:** Accepted
- **Date:** 2026-06
- **Deciders:** Engineering

## Context

The portal needs diagnostics that make failures traceable across the MVC → API → handler → repository path, and it separately needs an accountable, queryable record of meaningful data-access and ingestion events. Operational diagnostics and business auditing have different consumers, retention needs, and integrity requirements, so conflating them in one mechanism would be a mistake.

We needed:

- structured logs that can be shipped to a central sink;
- a correlation identifier that ties together every log line (and the client response) for a single request;
- consistent, non-leaking error responses; and
- a durable audit trail that survives independently of log verbosity/retention.

## Decision

Adopt **Serilog** for operational logging and a separate persisted **audit trail** for business events.

Operational logging:

- Serilog is configured (via `SerilogFileConfiguration` / `SerilogConfigurator`) with console + rolling-file sinks and environment/thread enrichers; a bootstrap logger captures start-up failures.
- `CorrelationIdMiddleware` resolves an inbound `X-Correlation-ID` (or generates one), stores it in `HttpContext.Items`, pushes it into the Serilog `LogContext`, and echoes it on the response.
- `UseSerilogRequestLogging()` emits one entry per request (method, path, status, elapsed) after the exception handler, so the final status is recorded.
- `LoggingBehaviour` (MediatR) logs the start, elapsed time, and outcome of every request.
- `GlobalExceptionHandlingMiddleware` maps exceptions to RFC 7807 `ProblemDetails` (validation → 400, domain → 400, otherwise → 500 with details suppressed outside Development), each stamped with the correlation ID.

Business auditing:

- `IAuditService` / `AuditService` persists immutable `AuditTrail` rows (actor, action, correlation ID, timestamp, optional entity/parameters/IP) via the Unit of Work.
- Audit failures are caught and logged — they must never break the primary request.

## Consequences

**Positive**

- End-to-end traceability: one correlation ID spans middleware, pipeline, error responses, and the client.
- Structured logs are ready for a central sink (Seq / Application Insights / ELK).
- Errors are consistent and never leak internals to clients.
- The audit record is durable and queryable, independent of log retention.

**Negative / costs**

- Two write paths (logs and audit) to understand and maintain.
- Audit writes add a database round-trip per audited action (mitigated: failures are swallowed and logged).
- Correlation handling adds a small amount of middleware plumbing.

## Alternatives Considered

- **Default `Microsoft.Extensions.Logging` only.** Adequate, but Serilog's enrichers, structured sinks, and request logging are a better fit for centralised observability. Both coexist; Serilog is the sink.
- **Audit via logs (no audit table).** Cheaper, but log retention/sampling would compromise auditability, and querying logs for compliance is awkward. Rejected.
- **Audit as a MediatR behaviour.** Considered, but audited actions carry endpoint-specific context (action type, parameters), so explicit `AuditService` calls from controllers are clearer for the current set of endpoints.
