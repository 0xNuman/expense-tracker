# Recurring Transactions

## Model
A `RecurringRule` is a schedulable that auto-posts committed transactions on their due date. Recurring is per-tenant and last-mile currency follows the target account.

```
RecurringRule (AggregateRoot):
    RecurringRuleId : Guid
    TenantId : Guid
    Name : string (1..80)
    Enabled : bool
    RuleKind : enum (Income|Expense|Transfer)
    // Scheduling:
    Cadence : enum (Daily|Weekly|Monthly|LastDayOfMonth|Yearly|CustomCron)
    Interval : int (e.g. every 2 weeks)
    DaysOfWeek : byte[]?       // when Cadence=Weekly
    DayOfMonth : int?          // 1..31 (clamp to month length)
    MonthOfYear : int?         // when Cadence=Yearly
    StartDateUtc : DateOnly
    EndDateUtc : DateOnly?
    NextRunUtc : DateOnly       // computed/scheduled
    // Txn template:
    AccountId : Guid
    CounterpartAccountId : Guid?  // for Transfer
    CategoryId : Guid?
    AmountAccountCurrency : Money
    FxSnapshotId : Guid?         // NULL; snapshotted fresh at each posting if cross-currency
    MemoPattern : string?
    Tags : string[]
    // Behaviour:
    AutoPost : bool (true for Phase 1; later phases allow draft mode)
    GraceDays : int (default 0)  // skip posting if past due > N days, alert instead
    LastRunAt : DateTimeOffset?
    LastRunTxnId : Guid?
```
Invariants:
- Cannot disable the underlying account/currency; disabling the rule pauses.
- After each posting, `NextRunUtc = Cadence.Advance(NextRunUtc)`. If `NextRunUtc > EndDateUtc` → rule completes (`Completed=true`).
- Edits do not regenerate past instances.

## Worker
`IHostedService recurring-worker` polls every minute (`RecurringWorkerOptions.Tick = "00:01:00"`).
- Lease-based: acquire a Postgres advisory lock per rule so multiple instances don't double-post.
- Posts transactions atomically per rule, attaching `RecurringExecutionLog` row (`(RuleId, ScheduledForUtc, PostedTxnId, Status: Posted|Skipped|Error, Error)`).
- Backoff: if posting fails, retry 3x with 30 s delay, then mark rule `Errored`, surface on tenant's "needs attention" surface.

## Stories

### US-REC-1 Define a monthly rent expense
**As** any Admin/Owner
**I want** to schedule a monthly rent payment
**So that** my ledger reflects it without manual entry.

Acceptance:
- `POST /api/tenants/{tenantId}/recurring-rules { name:'Rent', kind:'Expense', cadence:'Monthly', dayOfMonth:1, accountId, categoryId, amount, memo }` → 201 with HAL `_links['pause']`, `_links['history']`, `_links['post-now']`.
- Validation rejects `dayOfMonth` outside 1..31.
- First instance posts on next tick after `NextRunUtc`.

### US-REC-2 Define a salary income
Same as US-REC-1 with `kind:'Income'`.

### US-REC-3 Schedule a transfer
- `kind:'Transfer'` requires `counterpartAccountId`. Posts a Transfer entity (see 08-transfers.md).

### US-REC-4 View upcoming schedule
**As** a member **I want** to see planned posts ahead 30 days.
Acceptance:
- `GET /api/tenants/{id}/recurring-rules/{ruleId}/forecast?horizonDays=90` → projected instances with dates and amounts. Server computes from cadence minus holiday logic (no holiday handling in Phase 1).

### US-REC-5 Pause and resume
- `POST /api/recurring-rules/{id}/pause`, `/resume`.
- Pausing clears `NextRunUtc` acknowledgement until resume recomputes next run from `now`.

### US-REC-6 Manual trigger
- `POST /api/recurring-rules/{id}/post-now { asOfUtc }` posts immediately (use case: user paid rent early). Logged as `TriggeredBy=Manual`.

## API sketches
- `GET /api/tenants/{tenantId}/recurring-rules?enabledOnly=true`.
- `POST /api/tenants/{tenantId}/recurring-rules`.
- `GET /api/recurring-rules/{id}` (HAL: `_links.history`, `_links.pause`, `_links.post-now`).
- `PATCH /api/recurring-rules/{id}` (limited fields; cadence changes recompute `NextRunUtc`).
- `POST /api/recurring-rules/{id}/pause`, `/resume`, `/post-now`.
- `GET /api/recurring-rules/{id}/forecast?horizonDays=90`.
- `GET /api/recurring-rules/{id}/history`.