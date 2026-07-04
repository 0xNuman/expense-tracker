# CSV Import / Export

## Import
Column-mapping wizard in the UI. Generic input format supported; bank-specific profiles deferred to a later phase.

### US-CSV-1 Map columns and import
**As** any member
**I want** to bulk import bank statements
**So that** I don't re-key history.

Acceptance:
- UI wizard:
  1. **Upload** — drag-drop .csv file, choose target account.
  2. **Preview** — first 10 rows parsed; column headers shown.
  3. **Map** fields to: occurredOn, amount, type (income|expense or sign-detect), memo, counterAccount (for transfers, optional), category guess (optional), tags (optional).
  4. **Dry run** — server parses all rows and returns validation findings with row-level errors/warnings WITHOUT persisting (`POST /api/accounts/{accountId}/import/preview`).
  5. **Confirm** — `POST /api/accounts/{accountId}/import { mapping, dryRunId }` writes rows atomically (transactional); returns summary `totalImported, voidedDuplicates, errors`.

- Duplicate detection by `(accountId, occurredOn, amount, memo)` within a tenant; duplicates are surfaced as warnings; user opts to import duplicates (creates as normal with tag `csv-duplicate`) or to skip.

### US-CSV-2 Transaction type inference
- If `type` column absent and amounts signed: positive → Income, negative → Expense. If counter-account column present and currency matches → Transfer.
- Explicit type column always wins.

### US-CSV-3 Category guessing (lightweight)
- User maps a category column value to a category id during import (e.g., "GROCERY" → Food > Groceries). Unknown values create placeholder "Uncategorised" with a tag `csv-category:{value}`.

### US-CSV-4 Export
- `GET /api/tenants/{id}/export?format=csv&type=all|transactions|transfers&from=&to=&currency=base` returns `-csv` attachment (or `base`-currency converted rows).
- Always emitted in tenant base currency for downstream use; native currency column available if `currency=native` passed.

### US-CSV-5 Bulk undo
- Each import returns a `BatchId`. `POST /api/import-batches/{id}/void` voids all transactions created by that import (use case: mistake). Logs `BatchId` in `audit_log`.

## API sketches
- `POST /api/accounts/{accountId}/import/preview` (multipart or JSON `Base64Csv`) → returns `dryRunId` + findings.
- `POST /api/accounts/{accountId}/import { mapping, dryRunId }`.
- `GET /api/tenants/{id}/export?format=csv&...` → CSV stream.
- `POST /api/import-batches/{id}/void`.

## Security
- File size limit 5 MB in Phase 1 (no bank profiles/no zips).
- Max rows per import 10,000 (server rejects beyond with `413`).
- Streaming parse; never load whole file into memory.