# ADR-001: Adopt Clean Architecture

- **Status:** Accepted
- **Date:** 2026-06
- **Deciders:** Engineering

## Context

The COVID-19 Analytics Portal must ingest external public-health data, expose analytics through a REST API, and present them through a server-rendered MVC site. The brief emphasises maintainability, testability, and clear separation of concerns, and explicitly calls for a REST API consumed by an MVC front-end.

We needed an architectural style that:

- keeps business rules independent of frameworks (EF Core, ASP.NET, MediatR) and of the volatile external MoH feed;
- makes the core logic testable without a database or web server;
- enforces boundaries structurally rather than by convention; and
- accommodates two presentation hosts (API and Web) without leaking infrastructure into the domain.

The main alternative on the table was a conventional layered/N-tier MVC application with EF Core used directly from controllers/services.

## Decision

Adopt **Clean Architecture** (Onion / Hexagonal) with four conceptual rings realised as five projects:

- `CovidAnalyticsPortal.Domain` (core) — entities, value objects, enums, domain exceptions, and the persistence abstractions `IRepository<T>` / `IUnitOfWork`. References nothing.
- `CovidAnalyticsPortal.Application` (inner) — CQRS queries/handlers, DTOs, validators, MediatR behaviours, and service contracts/implementations. References Domain only.
- `CovidAnalyticsPortal.Infrastructure` (outer) — EF Core, the MoH HTTP client, caching, audit, clock, logging, and background ingestion. Implements the inner abstractions.
- `CovidAnalyticsPortal.API` (outer) — versioned REST controllers, middleware, Swagger, rate limiting.
- `CovidAnalyticsPortal.Web` (outer) — MVC, consuming the API over HTTP via a typed client; references **no** other solution project.

The **dependency rule** (source dependencies point inward) is enforced by project references: inner projects do not reference outer ones, so violations fail to compile. Cross-cutting needs are inverted through interfaces defined in the inner layers and implemented in Infrastructure.

## Consequences

**Positive**

- Domain and Application logic is unit-testable in isolation (demonstrated by the service and validator tests).
- Frameworks are replaceable details; e.g. the persistence provider switches via configuration without touching the core.
- Clear, predictable file locations; concerns do not bleed across boundaries.
- The MVC↔API seam is preserved because Web cannot reference inner assemblies.

**Negative / costs**

- More projects and registration/boilerplate than a single MVC app.
- Mapping between domain types and DTOs/contracts adds indirection.
- Contributors must understand the dependency rule to place code correctly.

## Alternatives Considered

- **Conventional layered MVC with EF Core in controllers/services.** Less boilerplate and a faster start, but couples business logic to EF Core and ASP.NET, makes isolated testing harder, and weakens the API/Web boundary. Rejected as less maintainable for the required scope.
- **Vertical slice architecture.** Attractive for feature isolation, but the explicit domain model (rich value objects, invariants) and the shared persistence contract fit the layered Clean Architecture model more naturally here.
- **Microservices.** Unjustified operational complexity for a single read-centric portal; a modular monolith with clean internal boundaries is sufficient and can be decomposed later if needed.
