# Personas and Tenancy

## Personas
### Priya — Solo Tracker (primary)
A freelancer juggling multiple clients. Wants one personal workspace and a separate workspace shared with her spouse. Tracks expenses in INR and USD; receives income in USD. Will use receipt photos heavily once available.

### Mateo — Household Co-Manager
Shares finances with his partner. Wants a shared workspace where both can log expenses and see the same categories. Needs role separation (one Owner, one Member).

### Lin — Multi-Workspace Power User
Belongs to three workspaces (personal, shared investment club, side business). Demands fast workspace switching and per-workspace category trees and recurring rules.

## Tenancy model
A **Tenant** is an isolated workspace. A **User** may belong to many tenants via **TenantMemberships** with a role.

### Roles
| Role | Capabilities |
|---|---|
| Owner | Everything; delete tenant; transfer ownership; manage members. |
| Admin | Manage members (except Owner), categories, accounts, recurring rules, budgets. Cannot delete tenant. |
| Member | Create/edit own transactions and transfers, view shared data, manage own profile. |

### Tenant entity (rich)
```
TenantId : Guid (strongly typed)
Name : non-empty, max 100
CreatedByUserId : UserId
CreatedAt : UTC
Members : TenantMembership[]
```
Invariant: a tenant must always have exactly one Owner; demoting/removing the owner is rejected unless an Owner target is specified.

### TenantMembership entity
```
TenantId, UserId, Role, JoinedAt, InvitedBy (nullable)
```
Invariant: `(TenantId, UserId)` unique; soft delete supported but membership rows retained for audit.

## User entity
```
UserId, Email (unique, normalized), DisplayName, PreferredBaseCurrency (CurrencyCode),
TimeZone (IANA), PreferredLocale, PasskeyCredentials[], MagicLinkTokens[],
CreatedAt, LastLoginAt
```
Email is the canonical identifier; a user may change email via a verified flow; no username.

## Workspace switching
- The JWT access token carries a `tenant_id` claim for the **active** tenant.
- Switching is an explicit `POST /api/auth/switch-tenant` returning a new access token.
- All API calls are scoped exclusively to the active tenant via EF Core global query filter on `TenantId`.
- The UI exposes a workspace switcher in the top bar with avatar stacks (letter avatars from tenant names).

## Invitation flow
1. Owner/Admin enters invitee email.
2. If user exists: add a `PendingInvitation` row.
3. If not: create the user with a soft state (`IsPending = true`) and email a join link.
4. Invitee accepts: `POST /api/invitations/{id}/accept` adds membership and issues a token with that tenant active.

## Stories
### US-1.1 First-run tenant bootstrapping
**As** a new user
**I want** my first tenant auto-created on signup
**So that** I can start tracking immediately without a setup wizard.

Acceptance:
- After magic-link confirmation, a `Personal` tenant is auto-created with the user as Owner.
- The user is issued an access token with `tenant_id = new tenant` active.
- The personal tenant is mutable (rename, set base currency).

### US-1.2 Invite a member
**As** an Owner/Admin
**I want** to invite via email
**So that** my partner can share the workspace.

Acceptance:
- `POST /api/tenants/{id}/invitations` with `{email, role}` returns `202 Accepted` and a HAL link to the invitation.
- Owner cannot be invited as a second Owner (one-Owner invariant enforced).
- Re-inviting an existing member promotes role if higher, returns `409` if equal role.

### US-1.3 Switch workspace
**As** a multi-workspace user
**I want** to switch active tenant from the UI
**So that** I see only the relevant data.

Acceptance:
- UI lists tenants where the user is a member via `GET /api/tenants?filter=mine`.
- Switching calls `POST /api/auth/switch-tenant { tenantId }` and replaces the access token.
- If the active tenant membership is revoked mid-session, the next API call returns `403` with a HAL link to `reauthenticate`.

### US-1.4 Leave a tenant
**As** a Member
**I want** to leave a workspace
**So that** I can stop sharing data.

Acceptance:
- `DELETE /api/tenants/{id}/members/me` revokes own membership.
- Owner cannot leave without first transferring ownership (`POST /api/tenants/{id}/transfer-ownership`).
- Leaving invalidates the access token scoped to that tenant.

## API sketches
- `GET /api` → returns `_links.tenants`, `_links['create-tenant']`, `_links.auth.*`.
- `GET /api/tenants?filter=mine&page=1&pageSize=20`
- `POST /api/tenants { name, baseCurrency? }`
- `GET /api/tenants/{id}` (HAL with `_links.members`, `_links.invitations`, `_links.categories`, `_links.accounts`)
- `POST /api/tenants/{id}/invitations { email, role }`
- `POST /api/invitations/{id}/accept`
- `POST /api/tenants/{id}/transfer-ownership { toUserId }`
- `POST /api/auth/switch-tenant { tenantId }`