# Multi-Currency

## Strategy
- Per-user base currency (preference on User).
- Every transaction/transfer stores a **historic FX snapshot** at creation time (`FXSnapshot`).
- Aggregations convert each amount to user base currency via the **stored snapshot** — never recomputed using current rates, so historic reports are stable.
- Current FX rates are fetched via `IExchangeRateProvider` for fresh transactions and UI live previews; never used to enrich historic aggregations.

## FXSnapshot
```
FXSnapshot:
    SnapshotId : Guid
    FromCurrency : CurrencyCode
    ToCurrency : CurrencyCode
    Rate : decimal              // multiplier From → To
    FetchedAtUtc : DateTimeOffset
    Source : string (Source-aware: 'ECB'|'OpenExchangeRates'|'Manual'|'TenantOverride')
    Method : enum (DailyFix|SpotAtTime|UserEntered)
    // A snapshot is per rate. Lookups key by (From, To, FetchedAtUtc).
```
Invariant: stored snapshots are immutable.

## IExchangeRateProvider (pluggable)
```
interface IExchangeRateProvider {
    Task<decimal?> GetRateAsync(CurrencyCode from, CurrencyCode to, DateTimeOffset? asOfUtc, CancellationToken ct);
    Task<IReadOnlyCollection<RateQuote>> GetRatesAsync(CurrencyCode baseCurrency, DateTimeOffset? asOfUtc, CancellationToken ct);
}
```
- Dev: `ManualExchangeRateProvider` configured via `appsettings.Development.json` with a fixed rate table (or reads from an `FXRates` seeded table).
- Prod: implementation deferred to Phase 1 build; candidates: Frankfurter (ECB, free), exchangerate.host, Open Exchange Rates, or Fixer.io — all swap-compatible behind this interface.

## Configure-time defaults
- Tenant has no base currency (decisions log); user picks base in Settings. MVP default: USD. UI lets user change; changing recomputes the visible base but **never rewrites** stored snapshots.
- Each currency list should expose ISO 4217 with minor unit digits; formatting done via `Money.Format()` (locale-aware).

## Stories

### US-FX-1 Log a cross-currency transaction
**As** a member with a USD account buying EUR-priced item
**I want** to log the EUR amount and let the server capture rate
**So that** base-currency totals are accurate at date.

Acceptance:
- Transaction form: amount field shows account currency. If account currency ≠ user base currency, server fetches `IExchangeRateProvider.GetRate(currency, base, occurredOn)` and stores the `FXSnapshot`.
- If provider fails, the form requires manual rate entry (`exchangeRate` field highlight).
- UI shows live preview of base-currency equivalent in a non-editable caption ("≈ $98.42 today").

### US-FX-2 Switch base currency preference
**As** a user **I want** to change my base currency display.
Acceptance:
- Setting updates existing snapshot lookups are still original; new reports render in new base by converting via `snapshot.From → newBase` through triangulation if needed (most providers return direct rates).

### US-FX-3 Multi-currency dashboard cards
- Account cards show their native balance; total "Net worth" converts to base currency with rate source indicator.

### US-FX-4 Exchange-rate cache (server optimisation)
- EF/DB caches `GetRate` results in `CachedRate` table with `FetchedAtUtc, FromCurrency, ToCurrency, Source`. TTL: 6 hours.

## API sketches
- `GET /api/fx/rates?base=USD&asOf=...` — exposes live/snapshot rates (HAL with `_links.source`).
- `POST /api/fx/snapshot { from, to, rate, asOfUtc }` (Admin only) to manually inject a snapshot.

## Testing
- Manual provider returns known rates; integration tests assert snapshots are written and historic reports ignore later rate changes.