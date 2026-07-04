# Requirements Index

This folder captures all functional + non-functional requirements, decisions, and phased build plan for the Expense Tracker.

## Reading order
1. **00-overview** — vision, scope, glossary, decisions log.
2. **01-personas-and-tenancy** — tenants, users, memberships, roles, switching.
3. **02-authentication** — magic link + passkeys, token rotation, sessions.
4. **03-domain-model** — value objects, aggregates, invariants, events.
5. **04-accounts** — wallet types, balance read model, net worth.
6. **05-transactions** — income/expense entity, edit, void, aggregations.
7. **06-categories** — customizable hierarchical tree, categories API.
8. **07-recurring** — scheduling, worker, auto-post, forecast.
9. **08-transfers** — cross-account moves, net-worth neutrality.
10. **09-multi-currency** — FX snapshots, provider abstraction, base currency.
11. **10-budgets** — Phase 2 minimal + Phase 3 advanced.
12. **11-csv-io** — import wizard, export, batch undo.
13. **12-receipt-ocr** — Phase 4 upload + synchronous parse.
14. **13-rest-api** — HAL conventions, root link, error model, rate limits.
15. **14-architecture** — solution layout, vertical slices, EF Core, security.
16. **15-testing** — unit + slice integration, contract tests, CI.
17. **16-deployment** — Docker Compose local; production notes deferred.
18. **17-frontend** — React + Vite + Tailwind mobile-first UI; HAL client.

## Phase plan
- [Phase 1 — MVP](./phases/phase-1-mvp.md): tenants, auth, accounts, txns, categories, transfers, recurring, multi-currency, CSV I/O.
- [Phase 2 — Minimal Budgets](./phases/phase-2-budgets.md): monthly category budgets with live progress.
- [Phase 3 — Advanced Budgets](./phases/phase-3-advanced-budgets.md): custom periods, rollover, suggestions, alerts.
- [Phase 4 — Receipt OCR/LLM](./phases/phase-4-receipt-ocr.md): capture, parse via pluggable provider, confirm.

Each phase yields a functional, shippable application.

## Conventions
- Requirements are numbered (`NN-title.md`) for stable references in commits and discussions.
- Stories follow `US-<DOMAIN>-<n>` naming; acceptance bullets are normative.
- API sketches use HAL conventions; treat as illustrative until Phase 1 implementation freezes URLs (only rels are stable).
- Decisions log lives in `00-overview.md`; append-only, never rewrite history.

## How to use
- Treat these as living docs: refine acceptance bullets during Phase 1 build, but never silently change scope — append a new decision row in `00-overview.md` when constraints shift.
- Squads orient phase planning by the corresponding phase file; cross-references back into the numbered docs.
- Addenda belong in `docs/decisions/` (ADR) when architecture-shifting choices are made mid-build; not created by these initial docs.