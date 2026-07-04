# Transactions (Income / Expense)

## Entity
```
Transaction (AggregateRoot):
    TransactionId : Guid
    TenantId : Guid
    AccountId : Guid
    CategoryId : Guid?    // nullable for transfers? No — transfers are separate entity. For txns, optional "uncategorised" allowed.
    TransactionType : enum (Income|Expense)
    AmountAccountCurrency : Money          // physical amount recorded in account.cyrrency
    FxSnapshot : FXSnapshot                // null when accountCurrency == userBaseCurrency
    AmountUserBaseCurrency : Money         // snapshot; immutable once written
    OccurredOnUtc : DateOnly               // user-chosen date (sliced UTC, displayed in tenant tz)
    Memo : string? max 500
    Notes : string?
    Tags : IReadOnlyCollection<string>
    AttachmentIds : IReadOnlyCollection<Guid>  // (Phase 4 — receipts)
    IsVoided : bool
    VoidedById : Guid?
    VoidedAtUtc : DateTimeOffset?
    VoidingTransactionId : Guid?           // null unless this is a void-ing mirrored record
    CreatedAtUtc, CreatedByUserId, ModifiedAtUtc, ModifiedByUserId
```
Invariants:
- `Account.Currency == AmountAccountCurrency.Currency` — else 422.
- `Amount > 0` — sign implied by `TransactionType`.
- `OccurredOnUtc` must be ≤ today + 1d tenant tz unless user toggles "allow future-dated" (default off).
- Cannot modify a voided tx (422) except to un-void within the same ledger cycle (see below).
- Category, if set, must belong to the same tenant and not be archived.

## Void vs delete
Transactions and transfers are never hard-deleted. A void posts a mirrored counter-transaction tied via `VoidedById`/`VoidingTransactionId` so ledger remains balanced and audit intact.

### US-TXN-1 Log an expense
**As** any workspace member
**I want** to log an expense quickly from the dashboard
**So that** tracking friction stays low.

Acceptance:
- Form posts `POST /api/accounts/{accountId}/transactions { type: 'Expense', amount, currency?, categoryId?, occurredOn, memo?, tags?, exchangeRate? }`.
- `currency` defaults to account currency; if different, an FXSnapshot is required (either supply `exchangeRate`, or the server fetches via `IExchangeRateProvider` and stores snapshot).
- Returns `201 Created` with HAL `_links['void']`, `_links.category`, `_links.account`.
- UI quick-add accessible from mobile bottom bar (FAB).

### US-TXN-2 Log income
Same as above with `type: 'Income'`. `Income` increases account balance.

### US-TXN-3 Filter & list within account
**As** any member
**I want** paginated, filtered list
**So that** I can reconcile.

Acceptance:
- `GET /api/accounts/{accountId}/transactions?page=&pageSize=&from=&to=&type=&categoryId=&q=&sort=`
- Embedded collection in HAL `_embedded.item`; each item `_links.void`, `_links.category`.
- Sort options: `date|created|amount`.

### US-TXN-4 Edit a transaction
**As** the original author or any Admin
**I want** to fix mistakes
**So that** ledger stays accurate.

Acceptance:
- `PATCH /api/transactions/{id} { amount?, categoryId?, occurredOn?, memo?, tags?, exchangeRate? }` → 200.
- Edits to `Amount` or `OccurredOn` outside the current (open) ledger period reject with `409` (must void + new entry instead).
- Edits update `AmountUserBaseCurrency` only if `exchangeRate` provided (rate immutability rule: existing snapshot preserved unless explicit override with reason).

### US-TXN-5 Void a transaction
See void vs delete above.
- `POST /api/transactions/{id}/void { reason }` → 201 with mirrored `VoidingTransaction`.
- Voided txns excluded from default aggregations but listed under "show voided".

### US-TXN-6 Personal vs workspace view
**As** a member **I want** to see "mine" within a shared workspace.
Acceptance:
- `GET /api/transactions?mine=true` filters `CreatedByUserId = current user` (HAL `_embedded.author`).

## Aggregations
- `GET /api/tenants/{id}/spending?from=&to=&groupBy=category|day|week|month|tag&currency=base` — Phase 1 supports `category|day|month`.
- All aggregations use the snapshot `AmountUserBaseCurrency` to avoid FX drift.

## API sketches
See story sketches above.