# Domain Model

## Principles
- Rich domain objects in `src/Domain` encapsulate invariants and behaviour. No public setters on money/currency fields except through explicit methods.
- No anemic models; no God entities. Each entity exposes intent-named methods (`Account.Deposit(amount)`, `Transaction.AssignCategory(...)`).
- Persistence concerns live in EF Core configurations inside `src/Infrastructure/Persistence/Configurations`. Domain assembly has no dependency on EF Core.
- Strongly typed IDs (`TenantId`, `UserId`, `AccountId`, `TransactionId`, `CategoryId`, `TransferId`, `RecurringRuleId`) implemented as readonly records wrapping `Guid`. JSON serializes as a bare Guid string.
- Money represented as a value object `Money(amount, currency)` with `amount` as `decimal` (postgres `numeric(18,4)`) and `currency` as 3-letter ISO 4217 string.

## Value objects

### Money
```
record Money(decimal Amount, CurrencyCode Currency):
   + WithAmount(newAmount): Money
   + Add(other: Money): Money  // rejects mismatched currency
   + Subtract(other: Money): Money
   + ConvertTo(targetCurrency, rate: FXRate): Money
   // scale rounding to/from minor units handled in formatter
```
Invariant: `Amount` has ≤ 4 decimal places; `Currency` valid ISO 4217.

### FXRate
```
record FXRate(CurrencyCode From, CurrencyCode To, decimal Rate, DateTimeOffset FetchedAt, string Source):
   + Convert(amount): decimal  (Amount * Rate)
```
Rate is multiplier `From → To`. Stored per snapshot at transaction creation time.

### CurrencyCode
Strongly typed wrapper around ISO 4217 strings; validated against a known list at construction. Supports minor-unit digits and symbol. Used by Money, FXRate, BaseCurrency preferences.

## Entities (summary; details in dedicated files)

### Tenant
See `01-personas-and-tenancy.md`.

### User
See `01-personas-and-tenancy.md`.

### Account
See `04-accounts.md`.

### Category
See `06-categories.md`.

### Transaction (Income/Expense)
See `05-transactions.md`.

### Transfer
See `08-transfers.md`.

### RecurringRule
See `07-recurring.md`.

### Budget (Phase 2)
See `10-budgets.md`.

## Cross-cutting base types
```
abstract class AggregateRoot { IReadOnlyCollection<IDomainEvent> Events { get; } void AddEvent(...) }
interface IDomainEvent  // dispatched after SaveChangesAsync
abstract class Entity<TId> where TId : notnull
```
Domain events enable side-effects (audit, notifications, derived snapshots) without bloating entities.

## Audit
- All aggregates carry `CreatedAtUtc`, `CreatedByUserId`, `ModifiedAtUtc`, `ModifiedByUserId` via EF Core shadow properties populated by `SaveChangesInterceptor`.
- Soft delete via `IsDeleted` flag + global query filter where appropriate (categories, accounts). Transactions and transfers are never soft-deleted; instead they are **voided** (see `05-transactions.md`) to preserve audit history and net-worth reconciliation.

## Multi-currency enforcement
- Every Money on a transaction is stored with `CurrencyCode`; conversion to user base currency uses the FXSnapshot referenced by the transaction.
- Aggregations never co-mingle different currencies without an explicit conversion step (services must call `Money.ConvertTo(base, snapshot)`).

## Testing
- Pure domain tests per entity (invariants reject invalid state, side-effects publish correct events) — xUnit + FluentAssertions.
- Domain project contains zero infrastructure references; tests run in < 1 s.