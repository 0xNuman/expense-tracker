# Expense Tracker

Multi-tenant SaaS expense tracker. .NET 10 Minimal API with PostgreSQL + EF Core (Vertical Slice), fully hypermedia (HAL) REST API, plus a mobile-first React + Vite + TypeScript + Tailwind PWA. Receipt OCR/LLM ingestion arrives in a later phase.

See [`docs/requirements/README.md`](./docs/requirements/README.md) for the full requirements and [`docs/requirements/phases/phase-1-mvp.md`](./docs/requirements/phases/phase-1-mvp.md) for the current build phase.

## Quick start

### Backend (.NET 10)
```bash
dotnet build
dotnet run --project src/ExpenseTracker.Api --urls http://localhost:5000
```
- HAL root: <http://localhost:5000/api> (`application/hal+json`)
- OpenAPI document (dev only): <http://localhost:5000/api/openapi.json>
- Health: `/health/live`, `/health/ready`

### Frontend (React + Vite)
```bash
npm --prefix client install
npm --prefix client run dev
```
- App: <http://localhost:5173>
- Vite proxies `/api` and `/health` to the backend on `:5000`, so start the API first.

## Repository layout
```
src/ExpenseTracker.Api/        Minimal API host (Hal/, Endpoints/, Health/)
src/ExpenseTracker.Domain/     Rich domain objects (EF-free)
client/                        React + Vite + TS + Tailwind (mobile-first PWA)
docs/requirements/             Canonical requirements + phase plans
ExpenseTracker.slnx            .NET solution
```

## Status
Walking skeleton complete: HAL root + health endpoints served by the API, React client walks `/api` via the `HalClient`. Next slice: auth (magic link + passkeys) — see [`docs/requirements/02-authentication.md`](./docs/requirements/02-authentication.md).