# Request Flow Diagram

A read request — **Trend Analysis with filters** — traced from the browser through the MVC app, the REST API, the MediatR pipeline, and the database. This reflects the actual controllers, behaviours, handler, and service in the solution.

```mermaid
sequenceDiagram
    actor U as User
    participant B as Browser
    participant W as Web MVC<br/>TrendsController
    participant AC as ICovidApiClient
    participant MW as API Middleware<br/>(Correlation · Security · Exception · RateLimit)
    participant C as AnalyticsController (v1)
    participant M as MediatR
    participant L as LoggingBehaviour
    participant V as ValidationBehaviour
    participant H as GetTrendAnalysisQueryHandler
    participant S as AnalyticsService
    participant R as IUnitOfWork / EfRepository
    participant DB as EF Core / Database
    participant A as AuditService

    U->>B: Choose metric, state, date range
    B->>W: GET /Trends?metric&from&to&state
    W->>AC: GetTrendAsync(...)
    AC->>MW: GET /api/v1.0/analytics/trends (X-Correlation-ID)
    MW->>C: forward (correlation pushed to log context)
    C->>M: Send(GetTrendAnalysisQuery)
    M->>L: Handle (start timer, log request)
    L->>V: next()
    alt invalid input
        V-->>MW: throw ValidationException
        MW-->>AC: 400 ProblemDetails (errors + correlationId)
    else valid
        V->>H: next()
        H->>S: AnalyzeTrendAsync(metric, period, state)
        S->>R: FindAsync(predicate by date/state)
        R->>DB: parameterised AsNoTracking query
        DB-->>R: rows
        R-->>S: domain entities
        S-->>H: TrendDto (series + change + direction)
        H-->>V: TrendDto
        V-->>L: TrendDto
        L-->>M: TrendDto (log elapsed ms)
        M-->>C: TrendDto
        C->>A: RecordAsync(ViewTrends, ...)
        A->>R: AddAsync(AuditTrail) + SaveChangesAsync
        C-->>AC: 200 JSON
    end
    AC-->>W: TrendResponse
    W-->>B: Razor view + Chart.js renders
    B-->>U: Interactive trend chart
```

## Notes

- **Behaviour order:** `LoggingBehaviour` wraps `ValidationBehaviour`, which wraps the handler (registration order in `AddApplication()`). Requests are logged first, then validated.
- **Error path:** validation/domain failures surface as RFC 7807 `ProblemDetails` with the correlation ID; unexpected errors become a sanitised 500.
- **Audit:** the Dashboard and Analytics controllers record an `AuditTrail` entry after a successful query; audit failures are swallowed and logged so they never affect the response.
