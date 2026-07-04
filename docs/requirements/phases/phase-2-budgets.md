# Phase 2 — Minimal Budgets

> Target: monthly category budgets with live progress on the dashboard.

## Scope
- `Budget` aggregate (per `10-budgets.md` — Phase 2 section): tenant-scoped, single category, monthly cadence, no rollover.
- CRUD endpoints + HAL `_links.progress` per budget.
- Spending computation: Σ expense tx snapshots base amount within the budget window (current calendar month).
- UI:
  - New "Budgets" nav item.
  - Dashboard widget "Budgets at a glance" with up to 6 chips (status coloured).
  - Per-budget detail page with progress bar, remaining amount, list of contributing transactions (HAL `et:drill-down`).
  - Edit/archive actions.

## Out of scope (deferred to Phase 3)
- Custom periods (weekly/yearly, custom days).
- Rollover.
- Trailing average suggestions.
- Email/in-app alert thresholds (only visual chip in Phase 2).

## Build order
1. Domain: `Budget` aggregate with Phase 2 fields, validation, rolled-up spend query.
2. Slices: create, list, get-with-progress, archive, edit (name/amount/category).
3. Tests: validation rejections (duplicate overlapping, archived category target), progress math (tests span month boundaries).
4. UI: budgets page, dashboard widget, drill-down.
5. Acceptance: integration tests + Playwright "create budget → log txn → see progress".

## Definition of done
- New budget page reachable from HAL root via `et:budgets`.
- Manual log a dining expense under budgeted category → progress changes within seconds (optimistic UI + query invalidation).
- Cannot create a second active `Monthly` budget on the same category without archiving the old.
- Demo data seed creates sample budgets in dev mode.