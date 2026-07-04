# Authentication

## Approach
Passwordless from day one. Two parallel mechanisms:

1. **Magic link** — email-based, time-limited token; completes login after user clicks.
2. **Passkey (WebAuthn)** — platform-bound credential (Face ID / Touch ID / security key) for instant re-auth with no email round-trip.

Both shipped in Phase 1 using ASP.NET Identity in .NET 10 (passkey support is built-in).

## Flows

### F-AUTH-1 First signup / sign-in by magic link
1. UI: enter email → `POST /api/auth/magic-link { email }`.
2. Server: upsert user (`IsPending=true` if new), generate one-time token, email magic link to `https://app/login-complete?token=...`.
3. User clicks → UI calls `POST /api/auth/magic-link/verify { token }`.
4. Server: validates token (signed, single-use, TTL 15 min, not yet consumed), marks email verified, issues JWT access (15 min) + refresh (rotating, 30 day), and (if new) auto-creates a `Personal` tenant per US-1.1.
5. UI: stores refresh in HttpOnly cookie (issued by backend) and access token in memory; navigates to app.

### F-AUTH-2 Passkey registration
1. Logged-in user visits Settings → Security → "Add passkey".
2. UI: `POST /api/auth/passkeys/begin-registration { deviceLabel }` returns PublicKeyCredentialCreationOptions.
3. Browser: `navigator.credentials.create(options)` → attestation response.
4. UI: `POST /api/auth/passkeys/complete-registration { attestation }` stores credential and returns HAL confirmation.
5. Passkey stored per-user (not per-tenant) so it works across the user's workspaces.

### F-AUTH-3 Passkey sign-in
1. Login page: "Sign in with passkey" button (no email needed initially; supports usernameless via discoverable credentials).
2. UI: `POST /api/auth/passkeys/begin-auth` returns PublicKeyCredentialRequestOptions.
3. Browser: `navigator.credentials.get(options)` → assertion.
4. UI: `POST /api/auth/passkeys/complete-auth { assertion }` → server validates, issues tokens.

### F-AUTH-4 Token refresh & rotation
- Access token TTL 15 min (tenant claim active at issue time).
- Refresh token in HttpOnly Secure SameSite=Lax cookie, rotating; reuse detection revokes the family.
- Tenant switch (`POST /api/auth/switch-tenant`) issues a new access token scoped to the chosen tenant; refresh token unchanged.

### F-AUTH-5 Email change
- Initiate: `POST /api/auth/email-change { newEmail }`.
- Confirms destination via magic-link challenge; old email receives revocation notice.
- Passkeys and refresh tokens remain valid through the change.

## Security requirements
- Tokens signed with ECDSA P-256 keys rotated quarterly; kid header present.
- Magic link tokens: 32-byte cryptographically random base64url; ttl 15 min; single-use; hashed at rest (`SHA-256`).
- Passkey registration enforces RP ID = api host; origin allowlist enforced server-side.
- Refresh rotation detects reuse: revokes token family and notifies user via email.
- Magic link requests never reveal whether the email exists (constant response time).
- All endpoints require HTTPS (HSTS in prod); dev bypassed locally but logged.
- Rate limit: 5 magic-link requests per email per hour; 10 passkey attempts per 5 min per IP.

## Stories

### US-AUTH-1 Sign up via magic link (no password)
**As** a brand-new user **I want** to sign up with only my email
**So that** I avoid password friction.

Acceptance:
- New user submitting `POST /api/auth/magic-link { email }` → 204 No Content (same response for known/unknown).
- Email contains a 15-min-valid link pointing to `GET /login-complete?token=`.
- Successful verification issues tokens and creates a `Personal` tenant.
- Replay of consumed token returns 410 Gone.

### US-AUTH-2 Add passkey after first login
**As** an authenticated user **I want** to register a passkey
**So that** future logins skip email.

Acceptance:
- Button visible from Settings; navigable via HAL `_links['passkey-registration']`.
- Resulting credential usable on subsequent `/api/auth/passkeys/begin-auth` flow.
- Multiple passkeys allowed; user-set label displayed (e.g. "iPhone Face ID").

### US-AUTH-3 Sign in with passkey
**As** returning user **I want** a one-tap passkey login
**So that** auth takes < 5 s.

Acceptance:
- Discoverable credentials flow works without typing email first.
- If no passkey registered, server returns 404 with `_links.magic-link`.

### US-AUTH-4 Switch active tenant
**As** a user with multiple memberships **I want** to switch
**So that** my next API calls hit that tenant.

Acceptance:
- See Personas doc US-1.3.
- Switch returns 200 with new access token, refresh unchanged, refresh cookie still valid.

### US-AUTH-5 Recover access after losing passkey device
**As** a user without passkey access
**I want** to use magic link as fallback.

Acceptance:
- Magic link always available.
- After success, in Settings → Security, lost passkeys can be revoked; potentially offer "Revoke all sessions" (`POST /api/auth/sessions/revoke-all`).

### US-AUTH-6 Revoke all sessions
**As** an alarmed user **I want** to revoke every active session
**So that** I regain control.

Acceptance:
- Invalidates all refresh tokens for the user, all passkey-issued access tokens (cannot revoke stateless JWTs but short TTL makes them moot).
- Passkeys themselves retained.

## API sketches
- `POST /api/auth/magic-link { email }` → 204.
- `POST /api/auth/magic-link/verify { token }` → 200 `{ accessToken, tenant, _links }`.
- `POST /api/auth/passkeys/begin-registration { deviceLabel }` → `{ creationOptions }`.
- `POST /api/auth/passkeys/complete-registration { attestation }` → `{ _links }`.
- `POST /api/auth/passkeys/begin-auth` → `{ requestOptions }`.
- `POST /api/auth/passkeys/complete-auth { assertion }` → `{ accessToken, tenant, _links }`.
- `POST /api/auth/refresh` (cookie-based) → `{ accessToken }`.
- `POST /api/auth/switch-tenant { tenantId }` → `{ accessToken, tenant }`.
- `POST /api/auth/sessions/revoke-all` → 204.
- `GET /api/auth/sessions` (list active refresh tokens: device, lastSeenIp, lastSeenAt).
- `DELETE /api/auth/sessions/{id}`.
- `GET /api/users/me/passkeys`, `DELETE /api/users/me/passkeys/{id}`.