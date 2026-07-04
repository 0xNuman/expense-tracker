# Frontend

## Stack
- React 19 + Vite + TypeScript + Tailwind v4 (mobile-first), PWA-installable.
- TanStack Query for server-state caching and invalidation.
- Zustand for UI state (selected tenant, theme, drawer open state).
- React Router v7 (data mode) with route loaders routing off HAL `_links` when possible.
- **HalClient**: thin hand-rolled HAL walker:
  - Walks `GET /api` to discover root.
  - Resolves `_links[rel]` at runtime via `client.follow(rel, { params })`; no URL hardcoding.
  - Schema-validated via Zod for responses; page errors surface via `ErrorBoundary`.
  - Caches navigations inside TanStack Query keys derived from rels + params.
- Tailwind v4 with utility-first tokens; custom design system for spacing/radius.

## Design philosophy
- **Mobile-first, desktop-rich**: single responsive layout that adapts progressively.
  - Default mobile view: bottom tab nav (Home, Add (FAB), Reports, Recurring, Profile) + collapsible filters.
  - ≥ 768 px: drawer sidebar with tenant switcher + accounts list pinned; main content area expands.
  - ≥ 1280 px: two-column main (`primary` 8/12, `aside` 4/12) for reports and insights.
  - ≥ 1920 px: optimised for dense, three-column modal pattern (list | detail | side info).
- **Excellent UX principles**:
  - Single global "Add" FAB reachable within one tap/click anywhere.
  - Optimistic updates + rollback on failure; confirm via toast.
  - No clutter; progressive disclosure (filters + grouping behind a single button).
  - Skeleton placeholders; never blank placeholders during loading.
  - Currency + number inputs accept natural typing (`12.50`, `1,000`); server stays authoritative.
- **Design tokens** (Tailwind theme extension):
  - Surfaces: paper-α, paper-β, paper-γ.
  - Accents: income (positive/safe), expense (caution), transfer (info).
  - Status chips: `OnTrack|Warning|Over|Upcoming`.
- **Dark mode**: default system preference; preserves user override; never provide default through Java for fear of flicker (`useSyncExternalStore` + cookie.

## Architecture (client/)
```
client/
  src/
    main.tsx
    App.tsx                    // route outlet & ErrorBoundary
    routes/                    // file-based routing
    features/                  // mirror backend slices: accounts/, transactions/, ...
    components/                // reusable UI (cards, drawer, tenantSwitcher, moneyInput)
    hal/                       // HalClient, follow, parser, zodSchemas
    state/                     // Zustand stores
    i18n/                      // EN strings only for Phase 1
    styles/
    pwa/                       // manifest + service worker (Vite PWA plugin)
  tailwind.config.ts
  vite.config.ts
```

## Pages
- `/login` → magic link or passkey option.
- `/login-complete` → consume token from email link.
- `/` dashboard: account cards, net-worth, recent txns, upcoming recurring.
- `/accounts` and `/accounts/{id}` detail.
- `/transactions` smart list (filter, group, search). Also reachable per-account.
- `/transfers` cross-account list.
- `/categories` tree editor.
- `/recurring` rules manager with preview agenda.
- `/settings`: profile (base currency, tenant tz, locale), security (passkeys, devices), workspace switcher, tenant member admin.
- `/reports` (Phase 1 minimal): per-category and per-period spending, drill-down.

## Stories

### US-UI-1 Onboard within 180 seconds
After sign-up, dispatcher screens guide the user through: set base currency (prefilled), create first account (preset suggestion), seed default category tree (already created), log first expense.
Acceptance:
- Playwright end-to-end test passes.
- No page jump > 2 levels deep to reach any critical action.

### US-UI-2 Tenant switcher
- Available in sidebar/desktop and as a sheet in mobile; lists only tenants where the user is a member and highlights active. Switching shows a single optimistic shell spinner.

### US-UI-3 Money input UX
- Inputs accept `+12.50`, `1,000`, `-5.00`, `5` → displayed in fixed format on blur with currency symbol.
- Optional keyboard with numeric type on mobile; submit on Enter.

### US-UI-4 Transaction list / grouping
- Default group: `Today`, `Yesterday`, `This Week`, `Earlier`.
- Sticky date headers; swipe actions on mobile (swipe-left to void).

### US-UI-5 Accounts card layout
- Mobile: vertical stack of horizontal scroll-snap cards.
- ≥ 768px: sidebar list (selectable), main grid of accounts with mini balance chart for last 30 days (sparkline).

### US-UI-6 Reports/visualisations (Phase 1 minimal)
- Category breakdown donut + trends area chart for last 6 months (data via spending endpoint).
- Drill-down by category follows HAL `et:drill-down` link.

### US-UI-7 Accessibility
- WCAG 2.2 AA. Keyboard navigable, ARIA labelling, focus trapping modals; high contrast mode.

### US-UI-8 Performance
- Lighthouse Performance ≥ 90 on mobile mock data in Phase 1 ship.
- Code-split per route; bundle < 250 KB gzipped initial.
- PWA installable; offline shell shows cached last data + "offline" status pill.

## Hypermedia client sample
```ts
const hal = new HalClient({ base: '/api', fetch, queryClient });

await hal.follow('curies'); // bootstrap registry
const user = await hal.followResource<UserHal>('users');
const tenant = await hal.followResource('tenants', { params: { filter: 'mine' }});
const created = await hal.post(hal.link('et:create-tenant'), { name: 'Personal' });
const list = await hal.crawl(created._links['categories'], { embed: 'items' });
```
- `HalClient` resolves `templated:true` links via `expand(rel, params)`.
- Errors deserialise to custom `ApiError` with `_links` exposed for "back to safety" actions.