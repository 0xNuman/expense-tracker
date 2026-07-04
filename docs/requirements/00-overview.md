# Overview

## Vision
A multi-tenant SaaS expense tracker with a rich-domain .NET 10 backend (EF Core + PostgreSQL, Vertical Slice architecture) exposing a fully REST/HATEOAS (HAL) API, paired with a modern React + Vite + TypeScript + Tailwind mobile-first UI. Phase 1 ships a complete, usable web app; later phases add advanced budgets and receipt OCR/LLM ingestion.

## Goals
- RESTful API compliant with HAL hypermedia (`application/hal+json`) — every discoverable via a root link walk, no URL hardcoding.
- Pragmatic rich domain objects: money (with currency), accounts, transactions, transfers, categories, recurring rules — encapsulating invariants without heavy DDD ceremony.
- Mobile-first UI that also shines on large monitors (responsive layout with progressive disclosure).
- Pluggable seams for FX provider and receipt OCR/LLM so future phases don't require rewrites.
- Multi-tenant isolation from day one without runtime complications.

## Non-goals (MVP)
- Native mobile apps (web is responsive, installable PWA only).
- Bank direct-connect / Open Banking sync (deferred indefinitely).
- Multi-language UI (English only; locale plumbing exists but only EN strings ship).
- Off-budget investments tracking (this is an expense tracker, not a portfolio).
- Double-entry accounting ledger (transfers are a dedicated entity for net-worth neutrality, not a full ledger).

## Glossary
| Term | Definition |
|---|---|
| Tenant | Isolated workspace; owns accounts, categories, transactions, recurring rules, budgets. |
| TenantMembership | User-to-tenant link with role (Owner/Admin/Member). |
| Transaction | Income or Expense entry with money + currency snapshot + referenced category. |
| Transfer | Cross-account move; dedicated entity, net-worth neutral. |
| Account | Wallet/credit card/bank account with currency; balances computed from txns + transfers. |
| Category | Hierarchical user-defined node in the tenant's category tree. |
| RecurringRule | Scheduler producing instances on a cadence; auto-posts via background worker. |
| FXSnapshot | Historic exchange rate captured at transaction creation; never recomputed. |
| Base currency | Per-user preference used for aggregated views. |
| Passkey | WebAuthn credential bound to a device for passwordless auth. |
| Magic link | Time-limited emailed token used to complete login. |

## Out of scope (MVP, deferred to later phases)
- Phase 2: minimal category-level budgets.
- Phase 3: advanced budgets (custom periods, rollover, trailing averages).
- Phase 4: receipt OCR/LLM ingestion (synchronous backend, pluggable provider).

## Success metrics
- Phase 1 acceptance: a brand-new user can sign up via magic link, add a passkey, create a tenant, an account, a category tree (3 levels deep), and log 10 income/expense/transfer transactions in under 180 seconds — all postman tests + a Playwright smoke test passing.
- Transfer always net-worth neutral in tests.
- Historic reports never fluctuate when FX rates change (snapshot verification).

## High-level architecture
```
[ Browser (React/TS/Tailwind) ]
        |  application/hal+json
[ .NET 10 Minimal API ]
        ├─ Vertical Slices (src/Features/*)
        ├─ Shared Core Domain (src/Domain/*)
        ├─ Infrastructure (PostgreSQL / EF Core / Identity / FX / Email)
        └─ IHostedService recurring worker
[ PostgreSQL ] [ Papercut/SES SMTP ] [ FX API ] [ OCR/LLM (phase 4) ]
```

## Decisions log
| # | Decision | Date | Rationale |
|---|---|---|---|
| 1 | HAL over JSON:API/Siren | kickoff | De facto standard with broad library support; adequate for CRUD. |
| 2 | Multi-tenant with switchable workspaces | kickoff | Coordinates personal + shared + business use cases per persona. |
| 3 | Per-user base currency + rate snapshot | kickoff | Reports stable across FX moves; respects user choice. |
| 4 | Transfer as its own entity | kickoff | Net-worth-neutral invariant easier than paired-transactions. |
| 5 | Auto-post recurring via IHostedService | kickoff | Simple, observable; supports draft-mode toggle later. |
| 6 | Magic link + passkeys both in Phase 1 | kickoff | .NET 10 Identity ships passkey support; best UX baseline. |
| 7 | Vertical Slice architecture | kickoff | Cohesion per feature; thin shared domain. |
| 8 | xUnit + Testcontainers integration per slice | kickoff | Real Postgres behavior coverage without per-developer setup. |
| 9 | Docker Compose local dev only | kickoff | MVP needs only local running stack; cloud topology decided later. |
| 10 | React + Vite + TS + Tailwind, mobile-first | kickoff | Modern, fast HMR, large ecosystem; PWA installable. |
| 11 | Hand-rolled HAL walker + TanStack Query | kickoff | Explicit HATEOAS at the client; caching layer free. |
| 12 | Pluggable `IExchangeRateProvider` and `IReceiptParser` | kickoff | Defer concrete provider commitment to later phases. |