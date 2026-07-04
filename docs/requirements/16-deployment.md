# Deployment

## Phase 1 — Local Docker Compose
Single compose file runs everything needed for development and demo:
```yaml
services:
  postgres:
    image: postgres:17
    environment:
      POSTGRES_DB: expensetracker
      POSTGRES_USER: et
      POSTGRES_PASSWORD: et
    ports: ["5432:5432"]
    volumes: ["postgres_data:/var/lib/postgresql/data"]
    healthcheck: ...

  smtp:
    image: denas03/papercut-smtp  # local inbox viewer
    container_name: papercut
    ports: ["25:25", "8081:80"]   # SMTP on 25, web UI on 8081

  api:
    build: ./src/ExpenseTracker.Api
    environment:
      ConnectionStrings__Postgres: Host=postgres;Database=expensetracker;Username=et;Password=et
      Email__Provider: Smtp
      Email__Smtp__Host: smtp
      Email__Smtp__Port: 25
    depends_on: [postgres, smtp]
    ports: ["5000:8080"]

  client:
    build: ./client
    ports: ["5173:5173"]
    environment:
      VITE_API_BASE_URL: http://localhost:5000/api

  otel-collector:  # optional
    image: otel/opentelemetry-collector-contrib
    volumes: ["./otel-collector.yml:/otel/config.yaml"]
    command: ["--config=/otel/config.yaml"]
```

### Compose profiles
- `dev` — bind mounts source, hot reload (.NET `dotnet watch`) and Vite HMR.
- `demo` — built images, no watch.
- `e2e` — Api + Postgres only, used by Playwright CI.

## Production topology (deferred; document only)
The committed infrastructure target is deferred until Phase 2 to avoid premature commitments. Candidates to evaluate:

1. Azure Container Apps + Azure Database for PostgreSQL Flexible Server.
2. AWS ECS Fargate + RDS Postgres + SES for email.
3. Kubernetes (self-hosted).

Decision gate: confirm expected load (single user vs 100 tenants) before Phase 2 build.

## Email in production
- `Email__Provider: Ses` with AWS region, access key via env vars.
- SPF/DKIM/DMARC configured for the sending domain before first prod deployment.

## Migrations
- `dotnet ef migrations add ...` generates SQL files checked in.
- `IHostedService` Migration host runs on startup; acquires advisory lock `et-migrate`; aborts if hashing mismatch (manual check needed).
- Pre-deploy hook in CI diff-migrations check: forbidden to apply destructive migrations without explicit label.

## Secrets & config
- Local dev: `dotnet user-secrets` for `Smtp`, dev FX keys.
- Compose: `.env` files not committed; CI uses GitHub Actions secrets.
- Production: environment variables (12-factor), no user secrets.

## Backups
- Phase 1 local only: postgres volume persists; `pg_dump` cron not yet automated.
- Phase 2+: nightly logical + physical backups of RDS/managed postgres; PITR enabled.

## Health & readiness
- `/health/live` returns 200 if process alive.
- `/health/ready` returns 200 if can query DB; 503 otherwise (used by compose / load balancer).
- `/api/health` HAL endpoint with provider-aware deep checks: DB, email, FX provider.