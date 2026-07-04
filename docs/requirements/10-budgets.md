# Budgets (Phase 2 minimal, Phase 3 advanced)

Budgets are intentionally out of Phase 1 (decisions log + dedicated question). Phase 1 ships reservable Envelope accounts and per-category spend overviews to bridge the gap.

## Phase 2 — Minimal category-level budgets
```
Budget (AggregateRoot):
    BudgetId : Guid
    TenantId : Guid
    CategoryId : Guid          // single category, can be leaf or branch (rollups use descendants)
    Name : string
    Amount : Money             // in tenant/user base currency; MVP supports base only
    Period : enum (Monthly)    // Period=Monthly only in Phase 2
    StartsOn : DateOnly        // month anchor (always day 1 in Phase 2)
    EndsOn : DateOnly?         // null = open-ended
    RolloverEnabled : bool = false (Phase 3 toggles)
    Notes : string?
```
- Status computed live: `Spent = Σ(expense tx snapshot base amount in period window)` filtered by category and descendants.
- Budget status enum: `OnTrack` (≤ 75%), `Warning` (75..100%), `Over` (>100%), `Upcoming` (zero spend so far).

## Stories — Phase 2

### US-BUD-1 Create a monthly category budget
**As** any Admin/Owner
**I want** to cap my dining-out spend
**So that** I stay aware.

Acceptance:
- `POST /api/tenants/{tenantId}/budgets { categoryId, name, amount, currency: base, period: 'Monthly', startsOn }` → 201 with HAL `_links.progress`, `_links.category`.
- Currency restricted to user/tenant base in Phase 2.
- Same categoryId may not have two overlapping `Monthly` budgets (validation; archiving the old one frees the slot).

### US-BUD-2 View budgets & progress
- `GET /api/tenants/{tenantId}/budgets?include=progress&page=&pageSize=` → HAL with `_embedded['budgets']` items each containing `{ spent, budgeted, pct, status, remaining }` and `_links.category`, `_links['spending-drill-down']`.
- UI: horizontal progress bars coloured by status; tappable to drill into relevant transactions.

### US-BUD-3 Edit / archive budget
- `PATCH /api/budgets/{id}` for non-overlapping fields; period changes reset `StartsOn`.
- `POST /api/budgets/{id}/archive`.

### US-BUD-4 Dashboard widget
- Widgets show status chips on dashboard with quick glances.

## Phase 3 — Advanced budgets

### US-BUD-P3-1 Custom period
- `Period` enum extended: `Weekly, Fortnightly, Monthly, Quarterly, Yearly, Custom`. `Custom` requires `PeriodDays`.
- Recompute cadence from arbitrary start date.

### US-BUD-P3-2 Rollover
- `POST /api/budgets/{id}/settle { asOf }` finalises a period: computes remaining and creates a `BudgetRollover` record referencing next period.
- Rollover tracking visible via `GET /api/budgets/{id}/periods`.

### US-BUD-P3-3 Trailing-average suggestions
- "Set budget based on trailing 6-month average spend" — computes average and pre-populates the create form.

### US-BUD-P3-4 Alerts
- Notification when reaching 75% / 95% / 100%. Delivered in-UI bell; optionally email (Phase 3+).

## API sketches
- `GET /api/tenants/{id}/budgets?include=progress`.
- `POST /api/tenants/{id}/budgets`.
- `GET /api/budgets/{id}` (HAL with `_links.progress`, `_links.category`, `_links.periods`).
- `PATCH /api/budgets/{id}`.
- `POST /api/budgets/{id}/archive`.
- `GET /api/budgets/{id}/progress?asOf=`.
- `GET /api/budgets/{id}/periods` (Phase 3).