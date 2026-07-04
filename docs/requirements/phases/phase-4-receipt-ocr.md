# Phase 4 — Receipt OCR / LLM

> Target: snap a receipt photo, get a suggested transaction, confirm.

## Scope
- Receipt upload + storage (Postgres `bytea` via `IReceiptStore`).
- Synchronous parse via pluggable `IReceiptParser` (per `12-receipt-ocr.md`).
- Provider abstraction + Admin config endpoint (`/api/admin/receipt-providers`).
- UI: camera capture on mobile (FAB "Scan"), review-and-confirm form, monetarize line items row preview.
- Store linked receipt URL on the transaction (HAL `_links.receipt`).
- Re-parse endpoint with optional `provider` query for re-runs.
- Common parsing heuristics: identify date, currency, total, tax, line items, merchant name.
- Category suggestioner (sample-trained or LLM-promoted) maps merchant text → existing tenant categories. Threshold below confidence → "Uncategorised".

## Out of scope
- Multiple receipts per transaction (allowed in data model but UI exposes only one-to-one in Phase 4).
- Bulk receipt import.
- Background async queue (synchronous phase; if response times force async, deferred to phase after).
- Provider key in-app editing (env-driven; admin endpoint just surfaces active + health).

## Build order
1. Add `Receipt` aggregate + EF config; bytea storage.
2. Implement `IReceiptParser` with a Mock provider returning a known shape (enables tests without provider keys).
3. Build HAL endpoints (upload, parse, list, download).
4. UI camera capture + review form (mobile-first) + receipt thumbnail on transaction detail.
5. Provider integration: pick provider (OpenAI GPT-4o / Claude / Gemini / Azure DI) per business decision; translate parser JSON into our domain shape.
6. Dedup by SHA-256 (409 with link to existing receipt).
7. Tests: parser mock returns valid; provider-call integration test gated (CI secret skipped if absent).
8. Acceptance: Playwright "upload sample receipt → confirm transaction → see receipt thumb".

## Definition of done
- New user uploads a sample JPEG receipt → within 30 s sees a confirm form pre-filled.
- Editing currency / line items updates the final transaction.
- Receipt link visible from the transaction detail page.
- Re-parse produces deterministic shape if same image re-uploaded (dedup blocks).
- All Phase 4 tests pass on CI; no provider key failures block CI (extracted-out).
- Lighthouse Performance still ≥ 90 on dashboards (no large payloads from receipt thumbnails — server scaled to ≤ 1024 px and WebP).
- Documentation: README updated with provider key setup + cost warnings.