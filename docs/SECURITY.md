# Security

This document summarises the security posture of the COVID-19 Analytics Portal as implemented, mapped to the OWASP Top 10 categories most relevant to a public, read-only analytics API.

## Data sensitivity

The portal processes **aggregate, public** Ministry of Health datasets. It stores **no personal data (PII)**. Audit entries record `actor`, `action`, `correlationId`, `timestampUtc`, and optionally `entityName`, `parameters`, and `ipAddress`.

## Controls in place

| Area | Control | Where |
|------|---------|-------|
| Input validation (A03) | FluentValidation on every query (coherent ranges, no future dates, known states, supported metrics), run in the MediatR `ValidationBehaviour` | `Application/**/Queries/*Validator.cs`, `Common/Behaviours/ValidationBehaviour.cs` |
| Injection (A03) | Parameterised EF Core LINQ only; no dynamic SQL | `Infrastructure/Persistence/Repositories/EfRepository.cs` |
| Security headers | CSP (`default-src 'none'; frame-ancestors 'none'`), `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`, strips `Server` | `API/Middleware/SecurityHeadersMiddleware.cs` |
| Error handling (A05/A09) | RFC 7807 `ProblemDetails`; internal details hidden outside Development; correlation ID on every error | `API/Middleware/GlobalExceptionHandlingMiddleware.cs` |
| SSRF (A10) | MoH base URL fixed via validated options (`ValidateDataAnnotations` + `ValidateOnStart`); no user-controlled outbound URLs | `Infrastructure/DependencyInjection.cs`, `MohApiOptions` |
| Abuse control | Per-IP fixed-window rate limit (100/min, HTTP 429) on all endpoints | `API/DependencyInjection.cs` |
| Transport | HTTPS redirection (both hosts); HSTS on Web outside Development | `API/Program.cs`, `Web/Program.cs` |
| Outbound resilience | Retry + timeout + circuit breaker on the MoH client | `Infrastructure/DependencyInjection.cs` |
| Traceability (A09) | Correlation ID propagated through middleware, logs, and responses; structured Serilog logging; durable audit trail | `CorrelationIdMiddleware`, `AuditService` |

## Configuration & secrets

- No secrets are committed. Configuration is environment-layered via `appsettings.{Environment}.json`.
- For production, supply connection strings and any secrets through environment variables or a secret store; never commit production overrides.

## Known gaps / hardening backlog

These are intentionally out of scope for the current read-only public portal and tracked as future enhancements (README §13):

- **CORS** is not yet restricted to the Web origin, and `AllowedHosts` is `*` in the shipped config.
- **Authentication/authorization** is not enforced; the audit endpoint and any future write operations should sit behind an `Admin` policy.
- **Dependency scanning** (`dotnet list package --vulnerable`) should be wired into CI.

## Reporting

For a real deployment, add a security contact and disclosure process here. This repository is an assignment artifact and does not host a live service.
