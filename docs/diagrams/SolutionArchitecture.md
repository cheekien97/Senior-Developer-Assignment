# Solution Architecture Diagram

The portal is a two-host modular monolith built with Clean Architecture. The MVC Web app consumes the REST API over HTTP; the API hosts the Application/Infrastructure layers around the Domain core. This diagram reflects the actual projects, services, and middleware in `src/`.

```mermaid
graph TD
    subgraph Client
        Browser["Browser<br/>Bootstrap 5 · Chart.js · flatpickr"]
    end

    subgraph Web["CovidAnalyticsPortal.Web (MVC)"]
        WebCtrls["Controllers<br/>Dashboard · Statistics · Trends · Audit · Home"]
        ApiClient["ICovidApiClient<br/>typed HttpClient"]
    end

    subgraph API["CovidAnalyticsPortal.API (REST)"]
        Corr["CorrelationIdMiddleware"]
        Sec["SecurityHeadersMiddleware"]
        Exc["GlobalExceptionHandlingMiddleware"]
        Rate["Rate limiter (per-IP fixed window)"]
        Ctrls["Controllers/V1<br/>Dashboard · Analytics · Audit"]
    end

    subgraph App["CovidAnalyticsPortal.Application"]
        Med["MediatR"]
        Beh["Behaviours<br/>Logging → Validation"]
        QH["Query Handlers"]
        Svc["DashboardService · AnalyticsService"]
        AuditSvcI["IAuditService · IMohDataProvider · IDateTimeProvider"]
    end

    subgraph Domain["CovidAnalyticsPortal.Domain"]
        Ent["Entities · Value Objects · Enums"]
        Abs["IRepository&lt;T&gt; · IUnitOfWork"]
    end

    subgraph Infra["CovidAnalyticsPortal.Infrastructure"]
        UoW["UnitOfWork · EfRepository&lt;T&gt;"]
        Ctx["AppDbContext (EF Core)"]
        Cache["IMemoryCache"]
        Moh["MohDataProvider + resilience handler"]
        Audit["AuditService"]
        Clock["SystemDateTimeProvider"]
        Bg["CovidDataSyncBackgroundService<br/>+ CovidDataImporter"]
    end

    subgraph External
        DB[("SQLite / SQL Server")]
        MoH[("MoH datasets<br/>github.com/MoH-Malaysia/covid19-public")]
        Logs[("Serilog sinks<br/>console + rolling file")]
    end

    Browser --> WebCtrls --> ApiClient
    ApiClient -->|HTTP/JSON| Corr
    Corr --> Sec --> Exc --> Rate --> Ctrls
    Ctrls --> Med --> Beh --> QH --> Svc
    QH --> Med
    Svc --> Abs
    Svc --> Ent
    Abs -.implemented by.-> UoW
    UoW --> Ctx --> DB
    Ctrls --> Audit
    Audit -.implements.-> AuditSvcI
    Audit --> UoW
    Moh --> Cache
    Moh -->|fetch CSV| MoH
    Bg --> Moh
    Bg --> UoW
    Beh --> Logs
    Exc --> Logs
    Corr --> Logs
```

## Notes

- **Dependency rule:** Web references no other solution project; it talks to the API only over HTTP. Domain references nothing; Application references Domain; Infrastructure + API reference Application.
- **Composition root:** `API/Program.cs` calls `AddApplication()`, `AddInfrastructure(configuration)`, and `AddApiServices()`, then applies migrations via `MigrateDatabaseAsync` before serving requests.
- **Pipeline order:** correlation → security headers → request logging → exception handling → HTTPS redirect → rate limiter → controllers.
