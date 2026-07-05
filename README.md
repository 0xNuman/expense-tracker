# Expense Tracker

Multi-tenant SaaS expense tracker. .NET 10 Minimal API with PostgreSQL + EF Core (Vertical Slice), fully hypermedia (HAL) REST API, plus a mobile-first React + Vite + TypeScript + Tailwind PWA. Receipt OCR/LLM ingestion arrives in a later phase.

See [`docs/requirements/README.md`](./docs/requirements/README.md) for the full requirements and [`docs/requirements/phases/phase-1-mvp.md`](./docs/requirements/phases/phase-1-mvp.md) for the current build phase.

## Quick start

### Prerequisites
- .NET 10 SDK
- Node 22+ (with npm) for the client
- Podman (for local Postgres + Papercut SMTP)

### Infrastructure (one-time per host boot on macOS)
```bash
podman machine start
podman compose up -d           # postgres on :5432, Papercut SMTP UI on :8081
```
- Postgres connection string: `Host=localhost;Port=5432;Database=expensetracker;Username=et;Password=et`
- Papercut web UI (view captured mail): <http://localhost:8081>

### Backend (.NET 10)
```bash
dotnet build
dotnet run --project src/ExpenseTracker.Api --urls http://localhost:5000
```
- Migrations are applied automatically on startup (advisory-locked). Disable via `Persistence:ApplyMigrationsOnStartup=false`.
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

### Tests
```bash
dotnet test                                # Runs both domain invariants and integration tests against Testcontainers
npm --prefix client run build              # client typecheck + build
```
Note: Ensure Docker/Podman is running before executing integration tests, as Testcontainers will spin up a transient Postgres instance.

### Secrets
All secrets required for local development are defaulted in `appsettings.Development.json`. You do not need to set any environment variables or user secrets for local runs.

## Repository layout
```
src/ExpenseTracker.Api/                 Minimal API host (Hal/, Endpoints/, Health/, Persistence/)
src/ExpenseTracker.Domain/              Rich domain objects (EF-free)
src/ExpenseTracker.Infrastructure/       EF Core: DbContext, configurations, migrations
tests/ExpenseTracker.Domain.Tests/       xUnit + FluentAssertions
client/                                 React + Vite + TS + Tailwind (mobile-first PWA)
docs/requirements/                      Canonical requirements + phase plans
docker-compose.yml                      Postgres 17 + Papercut SMTP (podman-compatible)
ExpenseTracker.slnx                     .NET solution
```

## Adding a migration
```bash
dotnet ef migrations add <Name> \
  --project src/ExpenseTracker.Infrastructure \
  --startup-project src/ExpenseTracker.Api \
  --output-dir Persistence/Migrations
```

## Status
Slice 2 (Persistence + Domain foundation) complete: strongly-typed IDs, Money/FXRate value objects, User/Tenant/TenantMembership aggregates, EF Core DbContext with Postgres configurations, Initial migration, advisory-locked `MigrationsHostedService`, 22 passing domain tests. Next slice: auth (magic link + passkeys) — see [`docs/requirements/02-authentication.md`](./docs/requirements/02-authentication.md).