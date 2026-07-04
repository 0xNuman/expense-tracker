# AGENTS.md — guidance for opencode sessions

## Project
Expense Tracker — .NET 10 Minimal API (PostgreSQL + EF Core, Vertical Slice) + React/Vite/Tailwind PWA. REST/HATEOAS (HAL). Phase 1 = MVP. See `docs/requirements/README.md` for full scope and `docs/requirements/phases/phase-1-mvp.md` for current phase.

## Repo layout
- `src/ExpenseTracker.Api/` — Minimal API host (Program.cs, Hal/, Endpoints/, Health/).
- `src/ExpenseTracker.Domain/` — rich domain objects (no EF dependency).
- `client/` — React + Vite + TS + Tailwind (PWA, mobile-first). HAL consumer in `client/src/hal/`.
- `docs/requirements/` — canonical requirements docs + phase plans.
- Tests: not yet scaffolded (planned: `tests/ExpenseTracker.Domain.Tests/`, `tests/ExpenseTracker.Api.IntegrationTests/`).

## Build & run
- Backend: `dotnet build` from repo root.
- Backend run (dev): `dotnet run --project src/ExpenseTracker.Api --urls http://localhost:5000`. OpenAPI doc at `http://localhost:5000/api/openapi.json` (dev only).
- Frontend: `npm --prefix client install` then `npm --prefix client run dev` (Vite on `:5173` proxies `/api` and `/health` to `:5000`).
- Frontend typecheck/build: `npm --prefix client run build`.
- Frontend lint: `npm --prefix client run lint` (oxlint).

## Testing (once added)
- `dotnet test` — unit + integration tests with Testcontainers Postgres.
- `npm --prefix client run test` — Vitest (planned).

## Conventions
- Backend: `TreatWarningsAsErrors=true`, camelCase JSON, HAL media type `application/hal+json`. No XML doc comments required (CS1591 suppressed).
- All API responses walk from HAL root `/api`; never hardcode URLs in the client.
- New slices go under `src/ExpenseTracker.Api/Features/<Feature>/` (Endpoints.cs, Requests.cs, Responses.cs, Handlers/, Validators.cs). See `docs/requirements/14-architecture.md`.
- Strongly typed IDs (`TenantId`, `AccountId`, ...) live in `src/ExpenseTracker.Domain`.
- Commit messages: short imperative summary; never commit secrets.

## Requirements discipline
Requirements live in `docs/requirements/`. When constraints change, append a row to the decisions log in `docs/requirements/00-overview.md` rather than silently rewriting history.

## Current status
Walking skeleton complete: HAL root + health endpoints served by .NET 10 API, React client walks `/api` via the HalClient. Next slice: auth (magic link + passkeys) — see `docs/requirements/02-authentication.md`.