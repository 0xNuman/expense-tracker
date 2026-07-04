# Architecture

## Solution layout (.NET 10)
```
ExpenseTracker.sln
src/
  ExpenseTracker.Api/                 // Minimal API host + endpoint registrations
  ExpenseTracker.Domain/              // No EF dependency. Pure rich domain + events
  ExpenseTracker.Application/         // Mediator-ish commands/queries, validation
  ExpenseTracker.Infrastructure/       // EF Core configs, identity, FX, email, persistence
  ExpenseTracker.Features/             // Vertical slices, one folder per feature
tests/
  ExpenseTracker.Domain.Tests/
  ExpenseTracker.Application.Tests/
  ExpenseTracker.Api.IntegrationTests/   // Testcontainers Postgres
docs/requirements/...
```

## Vertical Slice
Each feature folder contains:
- `Endpoint.cs` — one or more `Map*()` extension methods registering routes.
- `Handler.cs` — static or injected service that orchestrates domain + persistence.
- `Dto.cs` — input/output + Hypermedia builder for HAL responses.
- `Validator.cs` — FluentValidation rules.
- `Mapping.cs` optional — entity↔DTO mapping.

Sample slice: `Features/Accounts/`
```
Endpoints.cs        // MapAccounts(this IEndpointRouteBuilder)
Requests.cs         // CreateAccountRequest, AccountFiltersDto
Responses.cs        // AccountHalDto + AccountHalFactory
Handlers/           // CreateAccountHandler, ListAccountsHandler...
Validators.cs
```

## Domain core
- Strongly typed IDs as records implementing `IStrongId` so primitive-conversion EF Core conventions work uniformly.
- `AggregateRoot.Events` collected by `SaveChangesInterceptor` and dispatched to in-process handlers (e.g., audit writer) after commit.
- Domain package depends only on `MediatR.Contracts`-free interfaces (we keep dependencies out of Domain; we use plain C# events).

## EF Core 10 specifics
- Global query filters for `TenantId` automatically applied using `IEntityTypeConfiguration` + `ModelBuilder` convention; filter seeded by current `ITenantContext` (sliding value derived from JWT claim).
- Auditing shadow fields populated by `SaveChangesInterceptor`.
- Soft-delete query filter (`IsDeleted`) where applicable.
- Read models projected to non-tracked queries (`AsNoTracking()`); materialised views queried via raw SQL (`FromSqlInterpolated`) when needed.
- Migrations checked-in; `dotnet ef database update` run on app start using `IHostedService` in dequeue mode (idempotent, lock-based).
- Schema defaults to `et` and each aggregate root entity has its own table (no TPT/TPH magic).
- Use `bytea`, `numeric(18,4)`, `timestamptz` (for `DateTimeOffset`), `date` (`DateOnly`) and `jsonb` (for FXSnapshot failover only rarely).

## Authentication pipeline
- Cookie (HttpOnly, SameSite=Lax, Secure) for refresh token.
- Bearer JWT access token (15 min TTL) carry `sub`, `email`, `tenant_id`, `role_in_tenant`, scopes (`read`, `write`, `admin`).
- Passkey support via `Microsoft.AspNetCore.Identity`'s new PassKey APIs (.NET 10).
- Email sender via `IEmailSender<TUser>` abstraction:
  - Dev: Papercut local SMTP at `localhost:25` (Docker compose); captured messages visible in container logs and UI dev page.
  - Prod: AWS SES via AWS SDK (`AmazonSimpleEmailService`).
- Magic link token storage: hashed in `MagicLinkTokens` table.

## Background worker
- `RecurringWorker : BackgroundService` ticks each 60 s.
- Single-leader via Postgres advisory lock (`pg_try_advisory_lock`) keyed by `recurring-worker`.
- Each rule instance wrapped in an EF Core transaction.
- Outbox pattern optional for longer-running operations (CSV large import, OCR processing) — *not* required in Phase 1 (sync flows suffice).

## FX abstraction
See `09-multi-currency.md`. Wired via DI; production provider committed before Phase 1 ship.

## Frontend (separate Vite app)
See `17-frontend.md`. Served from `client/` directory; dev proxy points to API backend (`vite.config.ts`) port 5000; in production the API serves `client/dist` as static fallback.

## Configuration
- `appsettings.json` base.
- `appsettings.Development.json` master override for local running.
- Secrets via `dotnet user-secrets` (dev) or env vars (docker compose / prod). No secrets in source.
- Email, FX, database, OIDC settings namespaced under their own keys.

## Observability
- Serilog with structured logs to console + Postgres log sink.
- OpenTelemetry traces + metrics (exported to local OTLP collector in compose).
- `HealthCheckEndpoint` at `/api/health` (HAL link present).
- Per-tenant rate counters in Phase 2.

## Security boundaries
- Tenant context resolved once per request from JWT claim; cached in `ITenantContext`.
- Cross-tenant ops impossible via EF query filter; FK constraints also enforce `TenantId` to prevent stale URL access.
- Authorisation policies: `Admin`, `Owner`, `Member` enforced via attribute + endpoint filter.