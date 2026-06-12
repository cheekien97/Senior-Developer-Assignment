# COVID-19 Analytics Portal

> Analytics portal for Malaysian COVID-19 open data, built on **.NET 8** with **Clean Architecture**, **CQRS (MediatR)**, and a server-rendered **ASP.NET Core MVC** front-end that consumes an internal **ASP.NET Core REST API** over HTTP.
>
> **Data source:** Ministry of Health Malaysia open datasets — [`github.com/MoH-Malaysia/covid19-public`](https://github.com/MoH-Malaysia/covid19-public).

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean-1f6feb)](#6-architecture-overview)
[![Tests](https://img.shields.io/badge/Tests-44%20passing-2ea043)](#12-running-tests)

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Business Objectives](#2-business-objectives)
3. [Features Implemented](#3-features-implemented)
4. [Technology Stack](#4-technology-stack)
5. [Solution Structure](#5-solution-structure)
6. [Architecture Overview](#6-architecture-overview)
7. [Security Features](#7-security-features)
8. [Logging Strategy](#8-logging-strategy)
9. [Testing Strategy](#9-testing-strategy)
10. [Setup Instructions](#10-setup-instructions)
11. [Running the Application](#11-running-the-application)
12. [Running Tests](#12-running-tests)
13. [Future Enhancements](#13-future-enhancements)

Further documentation: [`docs/Architecture.md`](docs/Architecture.md) · [Architecture Decision Records](docs/adr/) · [Diagrams](docs/diagrams/).

---

## 1. Project Overview

The COVID-19 Analytics Portal ingests Malaysia's public-health datasets into a local, read-optimised store and surfaces them through three analytical experiences plus an audit log. It is built as two separate ASP.NET Core hosts:

- **`CovidAnalyticsPortal.API`** — a versioned REST API (the system of record for the portal's read models).
- **`CovidAnalyticsPortal.Web`** — a server-rendered MVC application that consumes the API over HTTP through a typed `HttpClient`.

A background service keeps the local store synchronised with the upstream Ministry of Health (MoH) datasets, so the portal renders real data with no manual database or import step.

| Page | Purpose |
|------|---------|
| **Dashboard** | National headline figures (total cases, deaths, active, recovered, plus new cases/deaths today) with a per-state breakdown and chart. |
| **Trend Analysis** | Time series of a chosen metric (cases, active, recovered, deaths), national or per-state, with start/end values, change, percentage change, and direction. |
| **State Statistics** | Filterable, tabular per-state daily figures over a date range. |
| **Audit Trail** | Read-only view of recorded data-access and ingestion events. |

---

## 2. Business Objectives

- **Make public COVID-19 data usable.** Turn raw daily MoH CSV snapshots into fast, filterable national and state-level analytics.
- **Stay responsive and resilient.** Serve analytics from a local store so the portal does not depend on the upstream feed at request time, and degrades gracefully when the feed is briefly unreachable.
- **Compute insight server-side.** Aggregate headline figures and derive trends (change, percentage change, direction) on the server rather than in the browser.
- **Be self-provisioning.** Apply the database schema and ingest data automatically at start-up so the solution runs on a clean machine with no manual setup.
- **Be accountable.** Persist an immutable audit trail of data-access and ingestion events, separate from operational logs.
- **Be maintainable and testable.** Use Clean Architecture and CQRS to keep business rules independent of frameworks, with a meaningful automated-test suite.

---

## 3. Features Implemented

- **National dashboard** — cumulative cases/deaths, active cases, recovered, new cases/deaths for the latest day in range, and a per-state breakdown ordered by new cases (`DashboardService`).
- **Trend analysis** — national or per-state metric series with computed start/end value, absolute change, percentage change, and direction (`Increasing` / `Decreasing` / `Stable`) derived in the domain (`TrendRecord`, `AnalyticsService`).
- **State statistics** — per-state daily figures over a date range, optionally filtered to a single state (`AnalyticsService.GetStateStatisticsAsync`).
- **Audit trail** — append-only `AuditTrail` records written by `AuditService` and exposed read-only via the API.
- **Automatic data ingestion** — `CovidDataSyncBackgroundService` runs shortly after start-up and then on a configurable interval; `CovidDataImporter` performs an idempotent upsert from `MohDataProvider`.
- **Automatic schema migration** — pending EF Core migrations are applied at API start-up (`MigrateDatabaseAsync`).
- **Versioned REST API** — `Asp.Versioning` URL-segment versioning (`/api/v1.0/...`) with versioned Swagger UI in Development.
- **Resilient outbound integration** — the MoH typed `HttpClient` uses the standard resilience handler (retry, timeout, circuit breaker).
- **Cross-cutting CQRS behaviours** — `LoggingBehaviour` and `ValidationBehaviour` run inside the MediatR pipeline.
- **Security middleware** — correlation IDs, security response headers, global `ProblemDetails` exception handling, and per-IP rate limiting.
- **Structured logging** — Serilog to console and rolling file, enriched with a correlation ID.

---

## 4. Technology Stack

| Concern | Technology |
|---------|-----------|
| Runtime | .NET 8 (LTS), C# with nullable reference types and implicit usings |
| Web (presentation) | ASP.NET Core MVC, Razor Views |
| API | ASP.NET Core Web API, `Asp.Versioning.Mvc` 8.1 |
| Mediation / CQRS | MediatR 12.4 (queries + pipeline behaviours) |
| Validation | FluentValidation 11.10 (executed inside the MediatR pipeline) |
| Persistence | EF Core 8.0.10 — SQLite (default) or SQL Server |
| Outbound resilience | `Microsoft.Extensions.Http.Resilience` 8.10 (retry, timeout, circuit breaker) |
| Caching | `Microsoft.Extensions.Caching.Memory` (`IMemoryCache`) |
| Logging | Serilog 4 (console + rolling file sinks, environment/thread enrichers) |
| API docs | Swashbuckle / Swagger 6.6 (versioned) |
| UI assets | Bootstrap 5, Bootstrap Icons, Chart.js, flatpickr (via CDN) |
| Testing | xUnit 2.5, FluentAssertions 6.12, Moq 4.20, `WebApplicationFactory`, in-memory SQLite, Coverlet |

See each project's `.csproj` for exact, pinned package versions.

---

## 5. Solution Structure

```
SeniorDeveloperAssignment/
├─ CovidAnalyticsPortal.sln
├─ README.md
├─ docs/
│  ├─ Architecture.md                      # Architecture & design reference
│  ├─ adr/                                  # Architecture Decision Records
│  └─ diagrams/                             # Mermaid diagrams
├─ src/
│  ├─ CovidAnalyticsPortal.Domain/          # Core — no outward dependencies
│  │  ├─ Common/                            # Entity, AuditableEntity, ValueObject
│  │  ├─ Entities/                          # CovidStatistic, StateStatistic, TrendRecord, AuditTrail
│  │  ├─ ValueObjects/                      # DateRange, StateCode, CaseMetrics
│  │  ├─ Enums/                             # MetricType, TrendDirection, AuditAction
│  │  ├─ Exceptions/                        # DomainException
│  │  └─ Repositories/                      # IRepository<T>, IUnitOfWork
│  ├─ CovidAnalyticsPortal.Application/     # Use cases — depends on Domain
│  │  ├─ Common/Behaviours/                 # LoggingBehaviour, ValidationBehaviour
│  │  ├─ Common/Interfaces/                 # IAnalyticsService, IDashboardService, IMohDataProvider, IDateTimeProvider, IAuditService
│  │  ├─ Dashboard/ Statistics/ Trends/ Audit/   # Queries, handlers, validators, DTOs
│  │  ├─ Services/                          # DashboardService, AnalyticsService
│  │  └─ DependencyInjection.cs
│  ├─ CovidAnalyticsPortal.Infrastructure/  # EF Core, MoH client, cache, Serilog, audit
│  │  ├─ Persistence/                       # AppDbContext, Configurations, Repositories, UnitOfWork, Migrations
│  │  ├─ ExternalServices/Moh/              # MohDataProvider, MohApiOptions
│  │  ├─ BackgroundServices/                # CovidDataSyncBackgroundService, CovidDataImporter, options
│  │  ├─ Audit/  Logging/  Time/
│  │  └─ DependencyInjection.cs
│  ├─ CovidAnalyticsPortal.API/             # REST API — versioning, Swagger, middleware
│  │  ├─ Controllers/V1/                    # DashboardController, AnalyticsController, AuditController
│  │  ├─ Middleware/                        # CorrelationId, SecurityHeaders, GlobalExceptionHandling
│  │  ├─ Context/  Swagger/  Logging/
│  │  └─ Program.cs
│  └─ CovidAnalyticsPortal.Web/             # MVC — consumes the API over HTTP
│     ├─ Controllers/                       # Dashboard, Statistics, Trends, Audit, Home
│     ├─ Models/                            # API contracts + view models
│     ├─ Services/                          # ICovidApiClient (typed HttpClient)
│     ├─ Views/                             # Razor + Bootstrap 5 + Chart.js
│     └─ wwwroot/
└─ tests/
   ├─ CovidAnalyticsPortal.Tests.Unit/         # Services, repositories, validators
   └─ CovidAnalyticsPortal.Tests.Integration/  # API endpoints (WebApplicationFactory)
```

---

## 6. Architecture Overview

The solution follows **Clean Architecture**: the domain sits at the centre with **zero outward dependencies**, and frameworks, the database, and the MoH feed are plug-in details on the outer ring. The dependency rule points strictly inward and is enforced by project references.

```
┌────────────────────────────────────────────────────────────┐
│  Presentation                                                │
│  CovidAnalyticsPortal.Web (MVC)  ──HTTP──▶  .API (REST)      │
├────────────────────────────────────────────────────────────┤
│  Infrastructure                                              │
│  EF Core · MoH HttpClient · IMemoryCache · Serilog · Audit   │
├────────────────────────────────────────────────────────────┤
│  Application                                                 │
│  CQRS handlers · DTOs · Validators · Pipeline behaviours     │
├────────────────────────────────────────────────────────────┤
│  Domain (core)                                               │
│  Entities · Value Objects · Enums · IRepository / IUnitOfWork│
└────────────────────────────────────────────────────────────┘
```

- **Domain** references nothing. It owns entities (`CovidStatistic`, `StateStatistic`, `TrendRecord`, `AuditTrail`), value objects (`DateRange`, `StateCode`, `CaseMetrics`), enums, and the persistence abstractions `IRepository<T>` and `IUnitOfWork`.
- **Application** references Domain only. It hosts CQRS queries/handlers, DTOs, FluentValidation validators, the MediatR behaviours (`LoggingBehaviour`, `ValidationBehaviour`), and the service contracts/implementations (`IDashboardService`/`DashboardService`, `IAnalyticsService`/`AnalyticsService`).
- **Infrastructure** references Application (and transitively Domain). It implements persistence (`EfRepository<T>`, `UnitOfWork`, `AppDbContext`), the resilient MoH integration, in-memory caching, the audit service, the system clock, and background ingestion.
- **API** references Application + Infrastructure. It exposes versioned REST controllers and owns the HTTP pipeline (correlation, security headers, exception handling, rate limiting, Swagger).
- **Web** references **none** of the other solution projects — it talks to the API purely over HTTP via the typed `ICovidApiClient`, preserving the architectural seam.

For full detail see [`docs/Architecture.md`](docs/Architecture.md) and the [diagrams](docs/diagrams/).

---

## 7. Security Features

Implemented in the solution today:

- **Server-side validation (A03).** Every query is validated by FluentValidation inside the MediatR `ValidationBehaviour`: date ranges must be coherent, end dates cannot be in the future, state filters must be recognised codes, and the metric must be supported.
- **No SQL injection (A03).** All data access goes through EF Core with parameterised LINQ; no string-concatenated SQL.
- **Security response headers.** `SecurityHeadersMiddleware` sets `Content-Security-Policy` (`default-src 'none'; frame-ancestors 'none'`), `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Permissions-Policy`, and removes the `Server` header.
- **Safe error handling (A05/A09).** `GlobalExceptionHandlingMiddleware` converts exceptions to RFC 7807 `ProblemDetails`: `ValidationException` → 400 with an `errors` dictionary, `DomainException` → 400, and any other exception → 500 with internal details suppressed outside Development. Every response carries a correlation ID.
- **SSRF protection (A10).** The MoH base address comes only from validated configuration (`IOptions<MohApiOptions>` with `ValidateDataAnnotations` + `ValidateOnStart`); the app never fetches arbitrary user-supplied URLs.
- **Rate limiting.** A per-IP fixed-window limiter (100 requests/minute, HTTP 429 on rejection) is applied to all API endpoints.
- **HTTPS redirection** is enabled on both hosts; HSTS is enabled on the Web host outside Development.
- **No PII.** The portal processes aggregate, public data only. Audit entries store actor, action, timestamp, correlation ID, and optional IP — no personal data.
- **Configuration hygiene.** No secrets are committed; configuration is environment-layered via `appsettings.{Environment}.json`.

See [ADR-004](docs/adr/ADR-004-Logging-and-Observability.md) and [`docs/Architecture.md`](docs/Architecture.md#8-security-design) for the full security design.

---

## 8. Logging Strategy

- **Structured logging with Serilog.** The API configures Serilog via `SerilogFileConfiguration` / `SerilogConfigurator` with console and rolling-file sinks and environment/thread enrichers. A bootstrap logger captures failures during start-up.
- **Correlation IDs.** `CorrelationIdMiddleware` reads an inbound `X-Correlation-ID` header (or generates one), stores it in `HttpContext.Items`, pushes it into the Serilog `LogContext` so it appears on every log line, and echoes it back on the response.
- **Request logging.** `UseSerilogRequestLogging()` records method, path, status code, and elapsed time for each request, positioned to capture the final translated status code.
- **Pipeline logging.** `LoggingBehaviour` logs the start, successful completion (with elapsed milliseconds), and any failure of every MediatR request.
- **Operational vs. audit logging are separate concerns.** Serilog output is for diagnostics; the business `AuditTrail` (persisted via `AuditService`) is a distinct, queryable record. Audit failures are caught and logged so they can never break the primary request.

---

## 9. Testing Strategy

The suite contains **44 tests** across two projects (**30 unit + 14 integration**), all passing.

| Suite | Targets | Approach |
|-------|---------|----------|
| **Unit — Services** | `DashboardService`, `AnalyticsService` | Moq-mocked `IUnitOfWork` / `IRepository`; deterministic clock |
| **Unit — Repositories** | `EfRepository<T>`, `UnitOfWork` | Real **in-memory SQLite** so EF configurations, converters and owned types are exercised |
| **Unit — Validators** | `GetDashboardQuery`, `GetStateStatisticsQuery`, `GetTrendAnalysisQuery` validators | `FluentValidation.TestHelper` |
| **Integration — API** | Dashboard, Analytics, and data-sync ingestion | `WebApplicationFactory<Program>` over the full HTTP pipeline with seeded in-memory SQLite |

Principles: Arrange-Act-Assert, deterministic time via an injected `IDateTimeProvider`, no real network calls (the integration factory disables outbound sync and seeds its own data), and `Method_Scenario_ExpectedResult` naming.

---

## 10. Setup Instructions

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A terminal (PowerShell on Windows; bash/zsh elsewhere)
- *(Optional)* Visual Studio 2022 17.8+ or VS Code with the C# Dev Kit

### 1. Restore and build

```bash
dotnet restore CovidAnalyticsPortal.sln
dotnet build CovidAnalyticsPortal.sln
```

### 2. Configuration

The API ships with working defaults in `src/CovidAnalyticsPortal.API/appsettings.json`:

```jsonc
{
  "Database": { "Provider": "Sqlite" },
  "ConnectionStrings": { "DefaultConnection": "Data Source=covidportal.db" },
  "MohApi": {
    "BaseUrl": "https://raw.githubusercontent.com/MoH-Malaysia/covid19-public/main/",
    "TimeoutSeconds": 30,
    "RetryCount": 3,
    "CacheMinutes": 60
  },
  "CovidDataSync": { "Enabled": true, "InitialDelaySeconds": 5, "IntervalHours": 12 }
}
```

To target **SQL Server**, override the provider and connection string (e.g. via environment variables or `appsettings.Production.json`):

```jsonc
{
  "Database": { "Provider": "SqlServer" },
  "ConnectionStrings": { "DefaultConnection": "Server=...;Database=CovidPortal;..." }
}
```

The Web app points at the API in `src/CovidAnalyticsPortal.Web/appsettings.json`:

```jsonc
{ "CovidApi": { "BaseUrl": "http://localhost:5114", "TimeoutSeconds": 30, "ApiVersion": "1.0" } }
```

### Zero-touch database & data initialisation

No manual database setup is required. On first start the API:

1. **Applies EF Core migrations** (`MigrateDatabaseAsync`) to create the SQLite schema (`covidportal.db`).
2. **Synchronises COVID-19 data** from the MoH open datasets via `CovidDataSyncBackgroundService`, which runs after `CovidDataSync:InitialDelaySeconds` and then every `CovidDataSync:IntervalHours` (default 12h). Ingestion is idempotent and resilient — upstream failures are caught, logged, and retried on the next interval, so the host never crashes.

> **Note:** the MoH dataset spans 2020–2022, so today's date may show zeros. Pass an explicit historical range, e.g. `GET /api/v1.0/dashboard?from=2021-06-01&to=2021-06-30`, to see populated figures.

---

## 11. Running the Application

Run the API and the Web app in two terminals.

```bash
# Terminal 1 — REST API
dotnet run --project src/CovidAnalyticsPortal.API --launch-profile http
# API:        http://localhost:5114
# Swagger UI: http://localhost:5114/swagger   (Development only)
```

```bash
# Terminal 2 — MVC Web app
dotnet run --project src/CovidAnalyticsPortal.Web --launch-profile http
# Web: http://localhost:5298   (opens on the Dashboard)
```

Default ports (from each project's `Properties/launchSettings.json`):

| Host | HTTP | HTTPS |
|------|------|-------|
| API | `5114` | `7135` |
| Web | `5298` | `7154` |

Start the **API first** so the Web app can reach it. Then browse to the Web URL and use the Dashboard, Trend Analysis, State Statistics, and Audit pages.

---

## 12. Running Tests

```bash
# Run everything
dotnet test CovidAnalyticsPortal.sln

# Run a single project
dotnet test tests/CovidAnalyticsPortal.Tests.Unit
dotnet test tests/CovidAnalyticsPortal.Tests.Integration

# Collect coverage (Cobertura)
dotnet test CovidAnalyticsPortal.sln --collect:"XPlat Code Coverage"
```

Generate a human-readable coverage report:

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"tests/**/coverage.cobertura.xml" \
  -targetdir:"tests/CoverageReport" -reporttypes:"Html;TextSummary" \
  -assemblyfilters:"+CovidAnalyticsPortal.*;-CovidAnalyticsPortal.Tests.*"
```

Expected result: **44 passing** (30 unit + 14 integration), 0 failed.

---

## 13. Future Enhancements

The core solution (Domain → Application → Infrastructure → API → Web → Tests) is complete and builds clean.

- **CORS** locked to the Web origin and `AllowedHosts` tightened per environment.
- **Authentication / authorization** for the audit endpoint and any future write operations (e.g. an `Admin` policy).
- **Architecture tests** (e.g. NetArchTest) to fail the build if the dependency rule is violated.
- **Containerisation** (a `Dockerfile` per host) and a CI/CD pipeline (build, test, coverage gate, vulnerability scan).
- **Centralised observability** — ship Serilog to a sink such as Seq / Application Insights and alert on errors and open-circuit events.
- **Gated migrations for multi-instance deploys** — apply migrations as a dedicated deploy step rather than at start-up to avoid concurrent-migration races.

---

<sub>Built with .NET 8 · Clean Architecture · CQRS · Data courtesy of the Ministry of Health Malaysia.</sub>
