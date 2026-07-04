# Transfers (Cross-Account Moves)

## Model
A dedicated entity (not a paired-transaction hack) so net-worth neutrality is intrinsic, query interfaces are clean, and HATEOAS links make sense.

```
Transfer (AggregateRoot):
    TransferId : Guid
    TenantId : Guid
    SourceAccountId : Guid
    DestinationAccountId : Guid
    AmountSourceCurrency : Money
    DestinationAmountCurrency : Money           // = amount after conversion
    FxSnapshot : FXSnapshot?                   // null if same currency
    OccurredOnUtc : DateOnly
    Memo : string?
    RefTransactionId : Guid?                   // mirror transaction created for legacy view (optional, off in Phase 1)
    IsVoided : bool
    VoidedById, VoidedAtUtc, ...
```
Invariants:
- `SourceAccountId != DestinationAccountId`.
- Both accounts belong to the same tenant (cross-tenant transfer is a Phase-N feature; not currently supported).
- Source and destination currencies must equal their respective account currencies.
- Voiding a transfer produces a mirrored counter-transfer, identical to transaction voids, never a hard delete.

## Net-worth neutrality
A transfer is invisible to tenant net-worth **in any single currency** because currency conversion still nets to zero when rates are consistent. Net-worth aggregation uses:
- Subtract `AmountSourceCurrency` from source account in source currency (already in account balance).
- Add `DestinationAmountCurrency` to destination in destination currency (already in account balance).
- When converting both to base currency for "net worth", use `DestinationAmount/FxRate` ≈ `SourceAmount` (rate may differ slightly, captured in `FxSnapshot`; minor FX gain/loss surfaces in FX-adjusted views only, Phase 3).

## Stories

### US-TRF-1 Move money between accounts
**As** any member
**I want** to transfer from one account to another
**So that** movements don't double-count as expense + income.

Acceptance:
- UI: FAB → "Transfer" pre-populated from anywhere; pick source, destination, amount, currency.
- `POST /api/tenants/{tenantId}/transfers { sourceAccountId, destinationAccountId, amount, currency, memo? }` → `201 Created`.
- If source currency ≠ destination currency, user enters destination amount OR exchange rate; server builds `FxSnapshot` via `IExchangeRateProvider` fallback.
- Returns HAL `_links.source`, `_links.destination`, `_links.void` and `_embedded.sourceAccount` summary, `_embedded.destinationAccount` summary.

### US-TRF-2 Same-currency transfer
No FXSnapshot recorded. Both amounts equal; UI hides FX fields.

### US-TRF-3 List and filter transfers
- `GET /api/accounts/{accountId}/transfers?direction=out|in|both&from=&to=`.
- `GET /api/tenants/{id}/transfers?accountId=&page=&pageSize=` for cross-account list.

### US-TRF-4 Void a transfer
- `POST /api/transfers/{id}/void { reason }` → 201 mirrored counter-transfer.

### US-TRF-5 Future cross-currency drift
- Source-to-destination rate preserved in snapshot. UI shows the rate explicitly so users understand differences. Phase 3 may surface FX gain/loss as a small report; not required in MVP.

## API sketches
- `POST /api/tenants/{tenantId}/transfers`.
- `GET /api/transfers/{id}` (HAL with embedded source/destination summary).
- `PATCH /api/transfers/{id} { memo?, occurredOn? }` (amount/account changes rejected; void & re-create).
- `POST /api/transfers/{id}/void { reason }`.
- `GET /api/accounts/{accountId}/transfers`.
- `GET /api/tenants/{tenantId}/transfers`.