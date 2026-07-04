# Phase 3 — Advanced Budgets

> Target: flexible periods, rollover, suggestions, alerts.

## Scope
- Custom periods: `Weekly, Fortnightly, Quarterly, Yearly, Custom` with `PeriodDays`.
- Period settlement: `POST /api/budgets/{id}/settle` finalises the current period; remaining (positive or overspend) carried via `BudgetRollover` linked to next period.
- Period tracking: `GET /api/budgets/{id}/periods` history of settled periods with rollover sums.
- Trailing averages: "Suggest budget amount" via `GET /api/tenants/{id}/spending?groupBy=category&window=trailing-6m` returns average; create form prefilled.
- Alerts: 75% / 95% / 100% thresholds trigger:
  - In-UI bell notification (`notifications` HAL collection surfaced in header).
  - Optional email digest (opt-in via Settings → Notifications).
- FX adjusted transfers report (per `08-transfers.md` doc): surfaces tiny FX gain/loss between transfer date and aggregated base conversion.

## Out of scope (deferred or undecided)
- Rollover caps (carry-over limits).
- Cross-category budget pools (e.g., total monthly spend cap regardless of categories).
- Shared budgets across workspaces.

## Build order
1. Extend `Budget` aggregate (periods, rollover, settles).
2. Slice updates: validate spans; service computes settled/in-progress/current.
3. Settle flow + UI on budget detail ("Settle period" button).
4. Trailing-average service + UI affordance.
5. Notifications aggregate + sink (in-UI + optional email).
6. FX-adjusted transfers report page (read-only).
7. Tests across period boundaries (weekly/quarterly), rollover math; play through year boundary.

## Definition of done
- Quarterly budget with rollover enabled rolls partial spending into next quarter.
- Notification fires at 95%.
- Trailing-average suggestion within 10% of real average on seeded data.
- All Phase 3 tests pass on CI; deployment unchanged (still Docker Compose local).