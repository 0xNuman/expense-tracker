# Categories

## Model
User-customizable hierarchical tree, per-tenant. Maximum depth: 4 (root excluded). Categories carry a kind that constrains transaction types.

```
Category (AggregateRoot):
    CategoryId : Guid
    TenantId : Guid
    ParentId : Guid?           // null = root-level
    Name : string (1..60, unique among siblings ignoring case)
    Kind : enum (Income|Expense|Either)
    Icon : string?             // emoji or icon key
    Color : string?            // hex
    SortOrder : int
    IsArchived : bool         // hidden from picker; existing txns retain FK
    Notes : string?
    // computed:
    Path : string             // "Food > Groceries > Produce" derived for display
    + Rename(newName)
    + ChangeKind(newKind)    // reject if any child mismatches
    + MoveTo(newParentId?)  // depth preserved, sibling-name uniqueness enforced
    + Archive() / Restore()
```
Invariants:
- Cannot create cycle (move under own descendant rejected).
- Cannot delete — only archive.
- A leaf `Expense` kind cannot become `Income` if any tx/transfers reference it; rejection message lists the count by type.
- Re-parenting must keep `Kind` consistent with new parent's `Kind` or `Either`.

## Stories

### US-CAT-1 Manage tree in UI
**As** any Admin/Owner **I want** drag-drop reordering and nesting **So that** categories match my mental model.

Acceptance:
- Sidebar tree view with collapse/expand; drag handle to reorder or reparent.
- Inline rename via `PATCH /api/categories/{id} { name }` (optimistic + HAL response).
- "Show archived" toggle; archived items grey-out with restore button.
- New category button under any parent → `POST /api/tenants/{tenantId}/categories { parentId?, name, kind, icon?, color? }`.

### US-CAT-2 Seed default tree on tenant creation
When a tenant is created, seed an opinionated-but-editable tree (Food, Transport, Bills, Income, Savings, Transfers-out placeholder). User can rename, archive, replace freely.

### US-CAT-3 Filter transactions by category
Transaction list accepts `categoryId` filter (see 05-transactions). Selecting a parent filters descendants. UI exposes breadcrumb filter chips.

### US-CAT-4 Reports by category (Phase 1 minimal)
- `GET /api/tenants/{id}/spending?groupBy=category` returns nested totals (parent rolls up children).
- Includes `_links['drill-down']` per category URI template `GET /api/tenants/{id}/spending?groupId=...`.

## API sketches
- `GET /api/tenants/{tenantId}/categories?includeArchived=false` (HAL with embedded tree via `_embedded['categories']`).
- `POST /api/tenant-s/{id}/categories { parentId?, name, kind, icon?, color? }`.
- `GET /api/categories/{id}` (HAL with `_links.parent`, `_links.children`, `_links.transactions` template).
- `PATCH /api/categories/{id} { name?, kind?, icon?, color?, sortOrder? }`.
- `POST /api/categories/{id}/move { newParentId?, newSortOrder }`.
- `POST /api/categories/{id}/archive`, `/restore`.