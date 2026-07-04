# Receipt OCR / LLM (Phase 4)

## Approach
Synchronous backend parsing. UI uploads receipt → backend calls pluggable `IReceiptParser` → returns a **proposed** transaction that the user reviews and submits.

```
interface IReceiptParser {
    Task<ReceiptParseResult> ParseAsync(IFormFile image, ReceiptParseOptions opts, CancellationToken ct);
}

record ReceiptParseResult:
    Confidence : float
    Merchant : string?
    IssuedAtUtc : DateTimeOffset?
    Currency : CurrencyCode?
    LineItems : ReceiptLineItem[]
    Total : Money?
    Tax : Money?
    SuggestedCategory : CategoryId?
    RawText : string              // for debug and "edit diagnosed" mode
    Warnings : string[]           // low confidence, multiple totals, unknown currency...
```
Concrete provider committed later (OpenAI GPT-4o, Anthropic Claude, Google Gemini, Azure Document Intelligence) — all behind the interface. Configuration picks via `appsettings`. Switchable per environment.

## Stores
Receipts stored as binary blobs (Postgres `bytea` in Phase 4 for simplicity; later phases can migrate to object storage behind an `IReceiptStore` interface).
```
Receipt:
    ReceiptId : Guid
    TenantId : Guid
    UploadedByUserId : Guid
    StorageRef : string
    ContentType : string
    Sha256 : string                // dedup key
    MappedTransactionId : Guid?
    ParsedResultId : Guid?
    CreatedAtUtc : DateTimeOffset
```
Parse results stored as `ReceiptParseResult` JSON.

## Flow
1. UI: attach photo → `POST /api/tenants/{tenantId}/receipts` (multipart) stores receipt row, returns HAL with `_links.parse`.
2. UI: `POST /api/receipts/{id}/parse` invokes `IReceiptParser` synchronously (5-15 s typical), returns the parser's proposed transaction.
3. UI: review; pre-filled new-transaction form; user can edit and submit (`POST /api/accounts/{id}/transactions`). Relationship recorded by `mappedTransactionId`.
4. Optional re-parse with a different provider (`?provider=openai` query).

## Stories

### US-OCR-1 Capture receipt via mobile
**As** a member
**I want** to snap a photo and auto-fill a transaction
**So that** tracking is effortless.

Acceptance:
- Mobile: FAB has a "Scan receipt" action open camera (via `navigator.mediaDevices` or file input with `capture="environment"`).
- Loading state shows progress spinner with cancel; timeouts after 30 s return server error and refresh retry link.

### US-OCR-2 Review and confirm
**As** a member
**I want** to correct OCR mistakes
**So that** my ledger stays accurate.

Acceptance:
- Parser's suggested fields populate the form; non-empty fields are highlighted as suggestion chips; line items shown as collapsible list; total step shows mismatch warning if sum ≠ total.
- User can switch currency if wrong.

### US-OCR-3 Provider management (Admin)
- `GET /api/admin/receipt-providers` lists active providers with health status.
- `POST /api/admin/receipt-providers/{key}/set-default`.

### US-OCR-4 Dedup
- Re-uploading an identical SHA-256 returns 409 with link to existing receipt and mapped transaction (if present).

## Security
- Max upload size 5 MB; mime `image/jpeg`, `image/png`, `image/webp`, `application/pdf`.
- Receipts visible only to tenant members via global query filter.
- Provider API keys in `appsettings.Production.json` (user secrets / env in dev); never logged.

## API sketches
- `POST /api/tenants/{tenantId}/receipts` (multipart upload).
- `GET /api/receipts/{id}` (HAL with `_links.parse`, `_links.download`).
- `POST /api/receipts/{id}/parse?provider=`.
- `GET /api/receipts` paginated with filters (e.g., unbound).
- `GET /api/users/me/receipts` (showing receipts mapped to user's txns).

## Notes
- Live provider keys configured in deployment; Phase 4 build phase decides provider.
- Falls back gracefully when no provider configured: returns 503 with explicit reason rather than crashing the UI.