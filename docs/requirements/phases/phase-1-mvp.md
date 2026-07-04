# Phase 1 — MVP

> Target: shippable full-stack app (backend + React UI) covering tenants, auth, accounts, transactions, categories, transfers, recurring, multi-currency, and CSV I/O.

## Scope
- Multi-tenant SaaS with switchable workspaces (per `01-personas-and-tenancy.md`).
- Passwordless auth: magic link + passkeys (per `02-authentication.md`).
- Accounts (per `04-accounts.md`).
- Transactions: income/expense with editable/void (per `05-transactions.md`).
- Categories: user-customizable tree, seeded defaults (per `06-categories.md`).
- Transfers (per `08-transfers.md`).
- Recurring transactions with auto-post worker (per `07-recurring.md`).
- Multi-currency with historic FX snapshots, pluggable provider (per `09-multi-currency.md`). **Provider choice committed during this phase build.**
- CSV import wizard + export (per `11-csv-io.md`).
- HAL hypermedia REST API root walkable from `/api` (per `13-rest-api.md`).
- React + Vite + TS + Tailwind UI mobile-first + desktop responsive (per `17-frontend.md`).
- Docker Compose local stack (per `16-deployment.md`).

## Out of scope
- Budgets (Phase 2).
- Receipt OCR/LLM (Phase 4).
- Open Banking / direct bank sync (won't-fix indefinite).
- Multiple language UI strings.

## Build order (each step keeps master shippable)
1. Walking skeleton: Django-like skeleton with .NET 10 Minimal API + Postgres + EF Core migrations + `/api` HAL root + health endpoints; client Vite scaffolded with `/login` stub.
2. Auth: magic link end-to-end (dev SMTP, `/login-complete`), token refresh, switch-tenant.
3. Tenants: create/list/invite/membership; seed personal tenant on signup.
4. Domain core: Money, FXRate, CurrencyCode, IDs; domain tests green.
5. Accounts slice: create, list, balance read model via SQL aggregate, archive/close.
6. Categories slice: tree CRUD, drag-drop in UI, seed defaults.
7. Transactions slice: income/expense list with filters, edit, void.
8. Transfers slice.
9. Multi-currency: provider interface + manual/dev provider + Postgres FXSnapshot persistence; UI rate preview.
10. FX real provider integration (block on committing to one of Frankfurter / exchangerate.host / Open Exchange Rates); switch via config.
11. Recurring: rules CRUD + IHostedService worker + execution log.
12. CSV import preview + commit + export + batch undo.
13. Reports (Phase 1 minimal): per-category and per-period spending with drill-down.
14. Passkeys: registration + login flows (WebAuthn).
15. UI polish: 180-s onboarding, PWA dark mode, Lighthouse ≥ 90.
16. Hardening: rate limits, CORS allowlist, problem+json, idempotency keys.
17. Acceptance: Playwright smoke, all integration tests green, README run steps.

## Out-of-scope but accessible ready behaviours
- Settings: base currency, locale, timezone, dev preferences.
- Workspace: invitations list, accept/reject flow.

## Definition of done
- Local `docker compose up --profile demo` boots working stack.
- New user onboards and logs 10 txns in < 180 s (Playwright).
- All integration tests pass on CI (Testcontainers Postgres).
- `/api` HAL root returns full link graph for an authenticated user.
- Historic report snapshot test: change live FX rate; verify totals unchanged for committed txns.
- Voiding txns/transfer keeps net-worth stable in tests.
- README documents local setup, secrets, and tests.

## Acceptance Criteria summary (per US referenced above)
See individual files for the canonical acceptance criteria. Each US must pass its acceptance bullets to qualify Phase 1 ship.