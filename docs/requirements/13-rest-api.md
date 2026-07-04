# REST API & HATEOAS

## Media type
All resource responses use `application/hal+json` (RFC: https://stateless.group/hal-spec/) unless binary. Errors use `application/problem+json` (RFC 7807) but include a `_links` object for navigability.

List collection responses use HAL's `_embedded.item`. Curie prefixes used for custom rels (`curies` link). Custom rels use vendor prefix `et` (e.g., `et:drill-down`).

## Conventions
- URL style: lower-kebab-case, plural resource roots (`/api/accounts/{id}/transactions`). No verbs in paths; actions on resources use `POST /resource/{id}/action`-style endpoints.
- HTTP verbs:
  - `GET` — read (idempotent, cacheable with ETag).
  - `POST` — create collection member or invoke action.
  - `PATCH` — partial update; JSON Merge Patch (RFC 7396) supported.
  - `PUT` — full replace (rare; not used in MVP).
  - `DELETE` — never used for txns (void instead). Used for passkeys, sessions, invitations rejection.
- Idempotency-Key header supported on `POST` actions that create aggregates (`transactions`, `transfers`, `recurring-rules`, `budgets`, `imports`). Dedupe by `(key, userId, 1h)`.
- Pagination: `page` (1-based) + `pageSize` (default 25, max 100). Collection response includes `_links.first/prev/next/last`, `total`, `page`, `pageSize`.
- Filtering: query params with `?filter=key:value,key2:value2` for advanced; simple keys (`from`, `to`, `accountId`, `categoryId`, `type`, `q`, `mine`) supported directly.
- Sorting: `?sort=name` asc, `?sort=-name` desc, multi-sort `?sort=name,-amount`.
- Time format: ISO 8601 UTC. Dates are `date`.

## Root endpoint
`GET /api` returns:
```json
{
  "_links": {
    "self": { "href": "/api" },
    "curies": [{ "name": "et", "href": "/docs/rels/{rel}", "templated": true }],
    "et:auth": {
      "magic-link": { "href": "/api/auth/magic-link", "title": "Request magic-link" },
      "passkey-begin-auth": { "href": "/api/auth/passkeys/begin-auth" },
      "passkey-begin-registration": { "href": "/api/auth/passkeys/begin-registration" },
      "refresh": { "href": "/api/auth/refresh" },
      "switch-tenant": { "href": "/api/auth/switch-tenant" }
    },
    "tenants": { "href": "/api/tenants?filter=mine" },
    "et:create-tenant": { "href": "/api/tenants", "method": "POST" },
    "et:fx-rates": { "href": "/api/fx/rates{?base,asOf}", "templated": true },
    "users": { "href": "/api/users/me" },
    "swagger": { "href": "/api/docs", "type": "text/html" }
  }
}
```

## Hypermedia link object shape
```
{ "href": "...", "method": "GET"|"POST"|..., "templated": bool, "title": "...", "type": "application/hal+json" }
```
- `method` defaults to `GET`.
- Template syntax uses RFC 6570 URI Templates.

## Error model
```json
{
  "type": "https://docs.expensetracker/errors/tenant-id-mismatch",
  "title": "Tenant mismatch",
  "status": 403,
  "detail": "The active tenant doesn't contain this resource.",
  "instance": "/api/accounts/abc",
  "traceId": "...",
  "_links": {
    "reauthenticate": { "href": "/api/auth/switch-tenant" }
  },
  "errors": {
    "accountId": ["The account does not belong to the active tenant."]
  }
}
```

## Status code matrix (subset)
| Case | Code |
|---|---|
| Validation error (DTO) | 422 |
| Concurrency / stale ETag | 412 |
| Invariant violation (domain) | 422 |
| Not found within tenant | 404 |
| Resource exists in another tenant only | 403 with `et:switch-tenant` hint |
| Duplicate (unique constraint) | 409 |
| Rate limited | 429 with `Retry-After` |
| Upload too large | 413 |
| Provider unavailable (FX, OCR) | 503 with `Retry-After` |

## Versioning
- No URL version. Additive changes only; breaking changes require a new media type (`application/hal+json; version=2`) negotiation via Accept header.

## OpenAPI
- Mini-spinner emits Swagger UI at `/api/docs` (HTML) and OpenAPI doc at `/api/openapi.json`.
- HAL shapes documented via per-resource examples.

## Security headers
- `Content-Type: application/hal+json` (negotiated by default).
- `Strict-Transport-Security` (prod).
- `X-Content-Type-Options: nosniff`.
- `Cache-Control: private, no-store` on auth + list endpoints; per-resource GET cacheable with ETag.
- CORS: origins allowlist (dev: `http://localhost:5173` Vite; prod: configured allowlist).

## Rate limits (global defaults; per-tenant overrides later)
- Anonymous auth endpoints: 60/min per IP for magic-link / passkey challenges.
- Authenticated general: 600/min per user.
- Heavy reads (export, report aggregations): 30/min per user.