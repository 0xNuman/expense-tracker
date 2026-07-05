import type { HalDocument, HalLink } from './HalClient';

/** A financial account belonging to the active tenant. */
export interface Account {
  id: string;
  name: string;
  type: string;
  currency: string;
  balance: number;
  openingBalance: number;
  isArchived: boolean;
}

/** A single transaction against an account. */
export interface Transaction {
  id: string;
  accountId: string;
  type: 'Income' | 'Expense';
  amount: number;
  currency: string;
  memo: string | null;
  occurredOn: string;
  isVoided: boolean;
  categoryId?: string | null;
}

/** Auth token bundle issued by verify / refresh / passkey completion. */
export interface AuthResult {
  accessToken: string;
  expiresAtUtc: string;
  tenantId?: string;
  tenantName?: string;
  email?: string;
}

export interface CreateAccountInput {
  name: string;
  type: string;
  currency: string;
  openingBalance: number;
}

export interface CreateTransactionInput {
  type: 'Income' | 'Expense';
  amount: number;
  currency: string;
  occurredOn: string;
  memo?: string | null;
  categoryId?: string | null;
}

const HAL_MEDIA = 'application/hal+json';

let rootCache: HalDocument | null = null;

/** Fetch the HAL root `/api` once and cache it for the app lifetime. */
export async function fetchRoot(signal?: AbortSignal): Promise<HalDocument> {
  if (rootCache) return rootCache;
  const res = await fetch('/api', { headers: { Accept: HAL_MEDIA }, ...(signal ? { signal } : {}) });
  if (!res.ok) throw new Error(`Failed to fetch /api: ${res.status}`);
  rootCache = (await res.json()) as HalDocument;
  return rootCache;
}

function pickLink(doc: HalDocument, rel: string): HalLink {
  const links = doc._links;
  if (!links || !(rel in links)) throw new Error(`HAL rel '${rel}' not present on document`);
  const v = links[rel];
  return Array.isArray(v) ? v[0] : v;
}

function authHeader(token?: string | null): Record<string, string> {
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export async function halFetch<T>(url: string, init: RequestInit, token?: string | null): Promise<T> {
  const res = await fetch(url, {
    ...init,
    headers: {
      Accept: HAL_MEDIA,
      'Content-Type': 'application/json',
      ...authHeader(token),
      ...(init.headers as Record<string, string> | undefined),
    },
  });
  if (!res.ok) {
    const body = await res.text().catch(() => '');
    throw new Error(`Request failed: ${res.status} ${res.statusText}${body ? ` — ${body}` : ''}`);
  }
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

function state<T = unknown>(doc: HalDocument, key: string): T {
  return (doc as Record<string, unknown>)[key] as T;
}

function embeddedItems(doc: HalDocument): HalDocument[] {
  const emb = doc._embedded;
  if (!emb) return [];
  const item = emb.item;
  if (!item) return [];
  return Array.isArray(item) ? item : [item];
}

/** List the accounts for the active tenant. */
export async function fetchAccounts(token: string): Promise<Account[]> {
  const root = await fetchRoot();
  const link = pickLink(root, 'et:accounts');
  const doc = await halFetch<HalDocument>(link.href, { method: (link.method ?? 'GET').toUpperCase() }, token);
  return embeddedItems(doc).map(toAccount);
}

function toAccount(doc: HalDocument): Account {
  return {
    id: state<string>(doc, 'id'),
    name: state<string>(doc, 'name'),
    type: state<string>(doc, 'type'),
    currency: state<string>(doc, 'currency'),
    balance: state<number>(doc, 'balance'),
    openingBalance: state<number>(doc, 'openingBalance'),
    isArchived: state<boolean>(doc, 'isArchived'),
  };
}

/** Create a new account. */
export async function createAccount(token: string, input: CreateAccountInput): Promise<Account> {
  const root = await fetchRoot();
  const link = pickLink(root, 'et:create-account');
  const doc = await halFetch<HalDocument>(
    link.href,
    { method: (link.method ?? 'POST').toUpperCase(), body: JSON.stringify(input) },
    token,
  );
  return toAccount(doc);
}

/** List transactions (all for tenant, or filtered to a single account when `accountId` given). */
export async function fetchTransactions(token: string, accountId?: string, tenantId?: string): Promise<Transaction[]> {
  const root = await fetchRoot();
  let href = pickLink(root, 'et:transactions').href;
  let method = 'GET';
  if (accountId) {
    href = `/api/accounts/${encodeURIComponent(accountId)}/transactions`;
    method = 'GET';
  }
  const doc = await halFetch<HalDocument>(href, { method }, token);
  const txns = embeddedItems(doc).map(toTransaction);

  if (tenantId) {
    try {
      const tHref = accountId 
        ? `/api/accounts/${encodeURIComponent(accountId)}/transfers`
        : `/api/tenants/${encodeURIComponent(tenantId)}/transfers`;
      const tDoc = await halFetch<HalDocument>(tHref, { method: 'GET' }, token);
      const transfers = embeddedItems(tDoc);
      
      for (const t of transfers) {
        const id = state<string>(t, 'id');
        const srcAccId = state<string>(t, 'sourceAccountId');
        const destAccId = state<string>(t, 'destinationAccountId');
        const srcAmt = state<number>(t, 'sourceAmount');
        const srcCur = state<string>(t, 'sourceCurrency');
        const destAmt = state<number>(t, 'destinationAmount');
        const destCur = state<string>(t, 'destinationCurrency');
        const occurredOn = state<string>(t, 'occurredOnUtc');
        const memoStr = (t as Record<string, unknown>).memo as string | null;
        const memo = memoStr ? `Transfer: ${memoStr}` : 'Transfer';
        const isVoided = state<boolean>(t, 'isVoided');

        if (!accountId || srcAccId === accountId) {
          txns.push({
            id: id + '_src',
            accountId: srcAccId,
            type: 'Expense',
            amount: srcAmt,
            currency: srcCur,
            memo,
            categoryId: null,
            occurredOn,
            isVoided
          });
        }
        if (!accountId || destAccId === accountId) {
          txns.push({
            id: id + '_dest',
            accountId: destAccId,
            type: 'Income',
            amount: destAmt,
            currency: destCur,
            memo,
            categoryId: null,
            occurredOn,
            isVoided
          });
        }
      }
    } catch (e) {
      console.error("Failed to fetch transfers for ledger", e);
    }
  }

  return txns.sort((a, b) => {
    // sort by date descending
    const d = b.occurredOn.localeCompare(a.occurredOn);
    // fallback to type or id to keep stable sort
    if (d !== 0) return d;
    return b.id.localeCompare(a.id);
  });
}

function toTransaction(doc: HalDocument): Transaction {
  return {
    id: state<string>(doc, 'id'),
    accountId: state<string>(doc, 'accountId'),
    type: state<'Income' | 'Expense'>(doc, 'type'),
    amount: state<number>(doc, 'amount'),
    currency: state<string>(doc, 'currency'),
    memo: (doc as Record<string, unknown>).memo as string | null,
    categoryId: (doc as Record<string, unknown>).categoryId as string | null,
    occurredOn: state<string>(doc, 'occurredOn'),
    isVoided: state<boolean>(doc, 'isVoided'),
  };
}

/** Record a new transaction against an account. */
export async function createTransaction(
  token: string,
  accountId: string,
  input: CreateTransactionInput,
): Promise<Transaction> {
  const href = `/api/accounts/${encodeURIComponent(accountId)}/transactions`;
  const doc = await halFetch<HalDocument>(href, { method: 'POST', body: JSON.stringify(input) }, token);
  return toTransaction(doc);
}

/** Request a magic-link login email. Resolves on 204. */
export async function requestMagicLink(email: string): Promise<void> {
  const root = await fetchRoot();
  const link = pickLink(root, 'et:auth');
  await halFetch(link.href, { method: (link.method ?? 'POST').toUpperCase(), body: JSON.stringify({ email }) });
}

/** Verify a magic-link token; returns tokens (refresh cookie is set automatically). */
export async function verifyMagicLink(token: string): Promise<AuthResult> {
  const root = await fetchRoot();
  const link = pickLink(root, 'et:auth-verify');
  const doc = await halFetch<HalDocument>(link.href, {
    method: (link.method ?? 'POST').toUpperCase(),
    body: JSON.stringify({ token }),
  });
  return {
    accessToken: state<string>(doc, 'accessToken'),
    expiresAtUtc: state<string>(doc, 'expiresAtUtc'),
    tenantId: state<string | undefined>(doc, 'tenantId'),
    tenantName: state<string | undefined>(doc, 'tenantName'),
    email: state<string | undefined>(doc, 'email'),
  };
}

/** Rotate the refresh cookie and issue a new access token. Cookie sent automatically. */
export async function refreshToken(): Promise<AuthResult | null> {
  const root = await fetchRoot();
  const link = pickLink(root, 'et:auth-refresh');
  const res = await fetch(link.href, {
    method: (link.method ?? 'POST').toUpperCase(),
    headers: { Accept: HAL_MEDIA, 'Content-Type': 'application/json' },
    credentials: 'include',
  });
  if (res.status === 401) return null;
  if (!res.ok) {
    const body = await res.text().catch(() => '');
    throw new Error(`Refresh failed: ${res.status}${body ? ` — ${body}` : ''}`);
  }
  const doc = (await res.json()) as HalDocument;
  return {
    accessToken: state<string>(doc, 'accessToken'),
    expiresAtUtc: state<string>(doc, 'expiresAtUtc'),
    email: state<string | undefined>(doc, 'email'),
    tenantId: state<string | undefined>(doc, 'tenantId'),
    tenantName: state<string | undefined>(doc, 'tenantName'),
  };
}

/** Begin WebAuthn passkey assertion for sign-in. Returns the challenge + session id. */
export async function beginPasskeyAuth(email?: string): Promise<{ sessionId: string; options: unknown }> {
  const root = await fetchRoot();
  const link = pickLink(root, 'et:passkey-begin-auth');
  const res = await fetch(link.href, {
    method: (link.method ?? 'POST').toUpperCase(),
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ email: email ?? null }),
  });
  if (!res.ok) throw new Error(`begin-auth failed: ${res.status}`);
  return (await res.json()) as { sessionId: string; options: unknown };
}

/** Complete WebAuthn passkey assertion; returns tokens (refresh cookie set automatically). */
export async function completePasskeyAuth(
  sessionId: string,
  assertion: unknown,
): Promise<AuthResult> {
  const res = await fetch('/api/auth/passkeys/complete-auth', {
    method: 'POST',
    headers: { Accept: HAL_MEDIA, 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ sessionId, assertionResponse: assertion }),
  });
  if (!res.ok) {
    const body = await res.text().catch(() => '');
    throw new Error(`complete-auth failed: ${res.status}${body ? ` — ${body}` : ''}`);
  }
  const doc = (await res.json()) as HalDocument;
  return {
    accessToken: state<string>(doc, 'accessToken'),
    expiresAtUtc: state<string>(doc, 'expiresAtUtc'),
    tenantId: state<string | undefined>(doc, 'tenantId'),
    tenantName: state<string | undefined>(doc, 'tenantName'),
    email: state<string | undefined>(doc, 'email'),
  };
}

/** Begin WebAuthn passkey registration (requires auth). */
export async function beginPasskeyRegistration(
  token: string,
  deviceLabel: string,
): Promise<unknown> {
  const res = await fetch('/api/auth/passkeys/begin-registration', {
    method: 'POST',
    headers: { Accept: 'application/json', 'Content-Type': 'application/json', ...authHeader(token) },
    credentials: 'include',
    body: JSON.stringify({ deviceLabel }),
  });
  if (!res.ok) throw new Error(`begin-registration failed: ${res.status}`);
  return res.json();
}

/** Complete WebAuthn passkey registration (requires auth). */
export async function completePasskeyRegistration(
  token: string,
  attestation: unknown,
  deviceLabel: string,
): Promise<void> {
  const res = await fetch('/api/auth/passkeys/complete-registration', {
    method: 'POST',
    headers: { Accept: HAL_MEDIA, 'Content-Type': 'application/json', ...authHeader(token) },
    credentials: 'include',
    body: JSON.stringify({ attestationResponse: attestation, deviceLabel }),
  });
  if (!res.ok) {
    const body = await res.text().catch(() => '');
    throw new Error(`complete-registration failed: ${res.status}${body ? ` — ${body}` : ''}`);
  }
}

export async function updateAccount(token: string, id: string, name: string, type: string, currency?: string): Promise<Account> {
  const href = `/api/accounts/${encodeURIComponent(id)}`;
  const body = currency ? { name, type, currency } : { name, type };
  const doc = await halFetch<HalDocument>(href, { method: 'PATCH', body: JSON.stringify(body) }, token);
  return toAccount(doc);
}

export async function archiveAccount(token: string, id: string): Promise<Account> {
  const href = `/api/accounts/${encodeURIComponent(id)}/archive`;
  const doc = await halFetch<HalDocument>(href, { method: 'POST' }, token);
  return toAccount(doc);
}

export async function voidTransaction(token: string, id: string, reason?: string): Promise<Transaction> {
  const href = `/api/transactions/${encodeURIComponent(id)}/void`;
  const doc = await halFetch<HalDocument>(href, { method: 'POST', body: JSON.stringify({ reason }) }, token);
  return toTransaction(doc);
}

export async function updateTransaction(token: string, id: string, input: Partial<CreateTransactionInput>): Promise<Transaction> {
  const href = `/api/transactions/${encodeURIComponent(id)}`;
  const doc = await halFetch<HalDocument>(href, { method: 'PUT', body: JSON.stringify(input) }, token);
  return toTransaction(doc);
}

export interface CreateTransferInput {
  sourceAccountId: string;
  destinationAccountId: string;
  amount: number;
  currency: string;
  destinationAmount?: number;
  memo?: string;
  occurredOn?: string;
}

export async function createTransfer(token: string, tenantId: string, input: CreateTransferInput): Promise<void> {
  const href = `/api/tenants/${encodeURIComponent(tenantId)}/transfers`;
  await halFetch<HalDocument>(href, { method: 'POST', body: JSON.stringify(input) }, token);
}