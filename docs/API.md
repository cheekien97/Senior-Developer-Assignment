# API Reference

The REST API is versioned via URL segment (`Asp.Versioning`); the current version is **v1.0**. In Development, an interactive Swagger UI is available at `/swagger`. All responses are JSON; errors use RFC 7807 `ProblemDetails` and carry a `correlationId` extension.

- **Base URL (dev):** `http://localhost:5114`
- **Versioned route prefix:** `/api/v{version}/...` (e.g. `/api/v1.0/dashboard`)
- **Rate limit:** 100 requests/minute per client IP (HTTP `429` on rejection)
- **Correlation:** send `X-Correlation-ID` to propagate your own; otherwise one is generated and returned

> Dates use ISO `yyyy-MM-dd`. The upstream MoH data spans 2020–2022, so use a historical range to see populated results.

---

## Dashboard

### `GET /api/v1.0/dashboard`

National headline summary plus per-state breakdown for the latest day in range.

| Query parameter | Type | Required | Notes |
|-----------------|------|----------|-------|
| `from` | `date` | No | Inclusive start; defaults applied when omitted |
| `to` | `date` | No | Inclusive end; defaults applied when omitted |

**Responses**

- `200 OK` — `DashboardDto`
- `400 Bad Request` — `ValidationProblemDetails`

**Example**

```http
GET /api/v1.0/dashboard?from=2021-06-01&to=2021-06-30
```

`DashboardDto` (fields): `asOfDate`, `periodStart`, `periodEnd`, `totalCases`, `totalDeaths`, `activeCases`, `totalRecovered`, `newCasesToday`, `newDeathsToday`, `stateBreakdown[]` (each a `StateStatisticDto`).

---

## Analytics

### `GET /api/v1.0/analytics/statistics`

Daily state-level statistics over a date range, optionally filtered to one state.

| Query parameter | Type | Required | Notes |
|-----------------|------|----------|-------|
| `from` | `date` | Yes | Inclusive start |
| `to` | `date` | Yes | Inclusive end (must be ≥ `from` and not in the future) |
| `state` | `string` | No | State code (e.g. `SGR`) or name (e.g. `Selangor`); omit for all states |

**Responses**

- `200 OK` — `StateStatisticDto[]` (ordered by state code, then date)
- `400 Bad Request` — `ValidationProblemDetails`

`StateStatisticDto` (fields): `stateCode`, `stateName`, `date`, `newCases`, `cumulativeCases`, `activeCases`, `recovered`, `newDeaths`, `cumulativeDeaths`.

### `GET /api/v1.0/analytics/trends`

Trend of a metric over a period, optionally scoped to a state, including the time series for charting.

| Query parameter | Type | Required | Notes |
|-----------------|------|----------|-------|
| `metric` | `enum` | Yes | One of `Cases`, `ActiveCases`, `Recovered`, `Deaths` |
| `from` | `date` | Yes | Inclusive start |
| `to` | `date` | Yes | Inclusive end |
| `state` | `string` | No | State code or name; omit for a national trend |

**Responses**

- `200 OK` — `TrendDto`
- `400 Bad Request` — `ValidationProblemDetails`

`TrendDto` (fields): `metric`, `stateName`, `periodStart`, `periodEnd`, `startValue`, `endValue`, `changeValue`, `changePercentage`, `direction` (`Increasing`/`Decreasing`/`Stable`), `points[]` (each `{ date, value }`).

---

## Audit

### `GET /api/v1.0/audit`

Read the append-only audit trail, optionally filtered, most-recent first.

| Query parameter | Type | Required | Notes |
|-----------------|------|----------|-------|
| `from` | `date` | No | Inclusive lower bound |
| `to` | `date` | No | Inclusive upper bound |
| `action` | `enum` | No | One of `ViewDashboard`, `ViewStatistics`, `ViewTrends`, `ViewHistory`, `ApplyFilter`, `ExportData`, `IngestData`, `SystemError` |
| `maxResults` | `int` | No | Defaults to `100` |

**Responses**

- `200 OK` — `AuditTrailDto[]`
- `400 Bad Request` — `ValidationProblemDetails`

`AuditTrailDto` (fields): `id`, `action`, `description`, `actor`, `correlationId`, `entityName?`, `parameters?`, `ipAddress?`, `timestampUtc`.

---

## Error format

All non-2xx responses follow RFC 7807. Example validation error:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "See the 'errors' property for details.",
  "errors": { "To": ["The end date cannot be in the future."] },
  "correlationId": "0f8c1d2e3a4b5c6d7e8f9a0b1c2d3e4f"
}
```
