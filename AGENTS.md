# AGENTS.md — guidance for opencode sessions

## Project
Expense Tracker — .NET 10 Minimal API (PostgreSQL + EF Core, Vertical Slice) + React/Vite/Tailwind PWA. REST/HATEOAS (HAL). Phase 1 = MVP. See `docs/requirements/README.md` for full scope and `docs/requirements/phases/phase-1-mvp.md` for current phase.

## Repo layout
- `src/ExpenseTracker.Api/` — Minimal API host (Program.cs, Endpoints/, Hal/, Health/, Persistence/).
- `src/ExpenseTracker.Domain/` — rich domain objects (no EF dependency): IDs, Money, FXRate, User, Tenant, TenantMembership.
- `src/ExpenseTracker.Infrastructure/` — EF Core DbContext, configurations, value converters, migrations.
- `tests/ExpenseTracker.Domain.Tests/` — xUnit + FluentAssertions domain invariant tests.
- `client/` — React + Vite + TS + Tailwind (PWA, mobile-first). HAL consumer in `client/src/hal/`.
- `docker-compose.yml` — Postgres 17 + Mailpit SMTP.
- `docs/requirements/` — canonical requirements docs + phase plans.

## Build & run
- Backend: `dotnet build` from repo root.
- Backend run (dev): `dotnet run --project src/ExpenseTracker.Api --urls http://localhost:5000`. OpenAPI doc at `http://localhost:5000/api/openapi.json` (dev only).
- Frontend: `npm --prefix client install` then `npm --prefix client run dev` (Vite on `:5173` proxies `/api` and `/health` to `:5000`).
- Frontend typecheck/build: `npm --prefix client run build`.
- Frontend lint: `npm --prefix client run lint` (oxlint).
- Domain tests: `dotnet test`.

## Infrastructure (Postgres + Mailpit via Docker)
```bash
docker compose up -d        # starts postgres on :5432 and Mailpit on :8025
docker compose down          # stop
```
Connection string: `Host=localhost;Port=5432;Database=expensetracker;Username=et;Password=et`
Mailpit web UI: <http://localhost:8025>

## Migrations
- Auto-applied on app startup via `MigrationsHostedService` (advisory-locked). Disable via `Persistence:ApplyMigrationsOnStartup=false`.
- Add a migration:
```bash
dotnet ef migrations add <Name> --project src/ExpenseTracker.Infrastructure --startup-project src/ExpenseTracker.Api --output-dir Persistence/Migrations
```

## Testing
- `dotnet test` — runs all tests (Domain.Tests; more suites added per phase).
- Domain unit tests run in < 1 s; no containers needed.
- `npm --prefix client run test` — Vitest (planned).

## Conventions
- Backend: `TreatWarningsAsErrors=true`, camelCase JSON, HAL media type `application/hal+json`. XML doc comments not required (CS1591 suppressed).
- Strongly typed IDs (`TenantId`, `UserId`, etc.) are readonly record structs wrapping `Guid`, implementing `IStrongId`.
- All API responses walk from HAL root `/api`; never hardcode URLs in the client.
- New slices go under `src/ExpenseTracker.Api/Features/<Feature>/` (Endpoints.cs, Requests.cs, Responses.cs, Handlers/, Validators.cs). See `docs/requirements/14-architecture.md`.
- Tenant scope: EF Core global query filter on `TenantId` (exclude `Tenant` itself); active tenant resolved per request from JWT claim via `ITenantContext` (placeholder `AmbientTenantContext` pre-auth).
- Commit messages: short imperative summary; never commit secrets.

## Requirements discipline
Requirements live in `docs/requirements/`. When constraints change, append a row to the decisions log in `docs/requirements/00-overview.md` rather than silently rewriting history.

## Decisions log highlights
See `docs/requirements/00-overview.md` for the full table. Key recent entries:
- #13 — Defer API versioning until a real breaking change (YAGNI; additive-only HAL changes).
- #14 — Tenant route id = GUID only (no slug for MVP); slug additive later if needed.

## Current status
**Phase 1 MVP Complete!** 
The full-stack app features switchable tenant workspaces, magic link & passkey auth, account management, multi-currency transactions, recurring rules, category trees, CSV import wizards, and an Apple-like premium UI with full PWA support. The Playwright E2E smoke tests successfully verify the < 180s onboarding flow.

Ready to begin **Phase 2 (Budgets)** or expand the `IntegrationTests` suite.

## Auth wiring (magic link)
- JWT access tokens signed with ECDSA P-256 via `JwtAccessTokenService` (singleton). Ephemeral key in dev; PEM key from config in prod.
- Refresh tokens stored hashed in `refresh_tokens` table, rotating on each refresh, family-based reuse detection.
- Magic-link tokens stored hashed in `magic_link_tokens` table, 15-min TTL, single-use.
- `AuthSetup.AddExpenseTrackerAuth()` wires: `IAccessTokenService` (eager singleton), `IEmailSender` (SmtpEmailSender to Mailpit), `ICurrentUserService` (scoped, reads JWT claims), `ITenantContext` (scoped, derives tenant from JWT claim), JWT bearer auth, authorization.
- Cookie helpers: `response.SetRefreshCookie(rawToken, expiresAt)`, `request.GetRefreshCookie()`, `response.ClearRefreshCookie()`. Cookie path `/api/auth`, HttpOnly, Secure, SameSite=Lax.
- Endpoints under `Features/Auth/AuthEndpoints.cs`: `POST /api/auth/magic-link`, `POST /api/auth/magic-link/verify`, `POST /api/auth/refresh`, `POST /api/auth/switch-tenant`.
- Dev: magic-link URL is logged when email sending fails (so tests can extract the token without Mailpit running).
- Passkeys: fully implemented (WebAuthn / simplewebauthn).