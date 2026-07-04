# Testing

## Strategy
Unit tests for rich domain objects + integration tests per slice. Integration tests use **Testcontainers for PostgreSQL** with the real Postgres image; no SQLite substitution (avoids behavioural drift, especially `'numeric'`, `'jsonb'`, advisory locks).

## Suites

### `ExpenseTracker.Domain.Tests`
- Pure entity invariants: void flows, money arithmetic, category tree cycle rejection, recurring cadence advance, transfer same-currency/cross-currency.
- Value objects: Money rounding, FXRate conversion, CurrencyCode validation.
- Aggregate event publication assertions.
- Aggregation helpers (spend computations) run against in-memory datasets.

### `ExpenseTracker.Application.Tests`
- Handler tests with mocked repositories (interfaces only) verifying orchestration and validating input.
- FluentValidation rule coverage.

### `ExpenseTracker.Api.IntegrationTests`
- `WebApplicationFactory<Program>` customised to swap DB connection string to Testcontainers Postgres.
- One fixture per major feature (auth, tenants, accounts, transactions, transfers, recurring, FX, CSV).
- HAL assertions: client receives expected `_links` containing the vehement rels and templated URIs.
- Per-test database reset via `Respawn` (schema-aware; chunks data faster than recreate).

## Conventions
- xUnit + FluentAssertions.
- Naming: `MethodName_Condition_ExpectedOutcome`.
- Test data via AutoFixture customisations for IDs; Bogus for rich text.
- Categorise via traits: `Category("Integration")`, `Category("Slow")`. CI runs all; local `dotnet test` accepts `--filter Category!=Slow`.

## Stories

### US-TST-1 Each slice has integration coverage
**As** a developer
**I want** auto-generated slice scaffolding to include a test file
**So that** coverage stays consistent.

Acceptance:
- New slice template adds `__SliceName__SliceTests.cs` with 1 happy-path and 1 invalid-input test.

### US-TST-2 HAL contract tests per resource
- Snapshot-style tests asserting rels for key resources (accounts list, transfer detail, recurring rule detail).

### US-TST-3 Domain invariants are unit-tested
- ≥ 10 invariant tests per aggregate.

### US-TST-4 Postgres behaviours verified
- Advisory lock tests (recurring worker concurrency).
- Numeric precision tests (trailing zeros preserved in formatters).
- Concurrent transaction write tests for invariants (e.g., double-void detection).

### US-TST-5 CI pipeline
- GitHub Actions workflow `.NET Test`: restores, builds, runs tests with Testcontainers (Postgres via service container). Required to merge to main.

### US-TST-6 Frontend test baseline
- Vitest for component unit tests (low ceremony).
- Playwright smoke test: "Sign up → create account → log 3 transactions → see updated balance → void one" added in Phase 1.

## Acceptance levels
- Pull request must maintain ≥ 70% line coverage on `ExpenseTracker.Domain` and `Features`.
- Slice handlers minimum 80% (orbital but excused branches logged).
- HalLinks contract tests count against the coverage target.