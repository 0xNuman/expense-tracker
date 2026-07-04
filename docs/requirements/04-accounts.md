# Accounts

## Purpose
An Account in Phase 1 represents any pool of money: a bank current account, savings, credit card, cash wallet, prepaid card, or a virtual envelope (sinking fund). Balances are derived from transactions and transfers (never stored denormalised without reconciliation).

## Entity
```
Account (AggregateRoot):
    AccountId : Guid
    TenantId  : Guid
    Name : string                    (1..60, unique within tenant)
    Type  : AccountType (enum: Cash|Checking|Savings|CreditCard|Prepaid|Envelope)
    Currency : CurrencyCode
    OpenedAtUtc : DateTimeOffset
    ClosedAtUtc : DateTimeOffset?    (nullable)
    OpeningBalance : Money
    InitialFxSnapshotId : Guid?      (if opening balance currency ≠ tenant/user base)
    Metadata : Dictionary<string,string>   (e.g. last4, bankName)
    IsArchived : bool                (hidden from default lists)
    // computed, not stored (unless materialised in a read model in Phase 2)
    + Deposit(amount: Money, fx?: FXRate)
    + Withdraw(amount: Money, fx?: FXRate)
    + Close(asOfUtc: DateTimeOffset)
    + Archive()
    + Rename(newName: string)
```
Invariant: An account currency is immutable after first transaction. Attempting to set per-transaction currency different from account currency is rejected; cross-currency flows must use a Transfer with explicit FXSnapshot.

## Account types
| Type | Direction convention |
|---|---|
| Cash | Income/Expense normal |
| Checking | Income/Expense normal |
| Savings | Income/Expense normal |
| CreditCard | Expenses positive increases balance owed; limit field |
| Prepaid | Like cash |
| Envelope | Virtual sinking fund; used for budgeting-style allocations in Phase 3 |

## Balance computation (read model)
`Balance = OpeningBalance + Σ(Income) − Σ(Expense) + Σ(TransfersIn) − Σ(TransfersOut)`
- For a credit card the "balance" is the amount owed; income deposits it.
- Balances are materialised in a Postgres view (`account_balances`) joined via EF Core read models in Phase 2. Phase 1 computes them on demand via SQL/EF query (acceptable for MVP scale).

## Stories

### US-ACC-1 Create account
**As** a Member or above
**I want** to create accounts in the active tenant
**So that** I can track different pools.

Acceptance:
- `POST /api/tenants/{tenantId}/accounts { name, type, currency, openingBalance }` → `201 Created` with HAL `_links.self`, `_links.transactions`, `_links.transfers`, `_links['close']`.
- Currency defaults to user base currency if omitted.
- Duplicate name within tenant → `409`.

### US-ACC-2 List accounts
**As** any workspace member
**I want** to list accounts with balances
**So that** I see overall position at a glance.

Acceptance:
- `GET /api/accounts?type=&includeArchived=false&page=&pageSize=` returns HAL collection embedded items with computed balance, `_links.transactions`, `_links.transfers`.
- Sort by `name|balance|createdAt`; default `name`.

### US-ACC-3 Rename / archive / close
**As** an Admin/Owner
**I want** to manage lifecycle
**So that** history is preserved without clutter.

Acceptance:
- `PATCH /api/accounts/{id} { name }` → 200; archived accounts excluded from default lists but visible when filtered.
- `POST /api/accounts/{id}/archive` and `/restore`.
- `POST /api/accounts/{id}/close { asOfUtc }` flips `ClosedAtUtc`; closed accounts reject new transactions (`POST /api/accounts/{id}/transactions` → `409`).

### US-ACC-4 Net worth card
**As** any member
**I want** to see total net worth in user base currency
**So that** I compare contexts.

Acceptance:
- `GET /api/tenants/{id}/net-worth` returns aggregated balances converted to the user's base currency using the most recent FX snapshots (or live rate fallbacks when historic snapshot absent).
- Result includes `byAccount[]`, `total`, `currency`, `_links.fxBreakdown`.

## API sketches
- `GET /api/accounts`
- `POST /api/accounts { name, type, currency, openingBalance }`
- `GET /api/accounts/{id}` (HAL with `_links.transactions`, `_links.transfers`)
- `PATCH /api/accounts/{id} { name }`
- `POST /api/accounts/{id}/archive`, `/restore`
- `POST /api/accounts/{id}/close { asOfUtc }`
- `GET /api/tenants/{tenantId}/net-worth`