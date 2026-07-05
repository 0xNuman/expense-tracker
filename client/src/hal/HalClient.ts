/**
 * Link object within a HAL document.
 * Matches the backend `Link` shape (camelCase JSON).
 */
export interface HalLink {
  href: string;
  method?: string;
  title?: string;
  type?: string;
  name?: string;
  templated?: boolean;
}

/** A HAL document: state properties plus `_links` and `_embedded`. */
export type HalDocument = Record<string, unknown> & {
  _links?: Record<string, HalLink | HalLink[]>;
  _embedded?: Record<string, HalDocument | HalDocument[]>;
};

const HAL_MEDIA = 'application/hal+json';

/**
 * Thin hand-rolled HAL walker.
 *
 * The client never hardcodes URLs. It starts at `/api` and follows
 * links by rel at runtime. Caching/invalidation will be layered on
 * via TanStack Query in a later step; this minimal client is enough
 * for the walking skeleton.
 */
export class HalClient {
  private readonly baseUrl: string;
  private readonly getAuthToken?: () => string | null;
  private readonly onUnauthorized?: () => void | Promise<void>;

  /**
   * @param baseUrl Root HAL endpoint, default `/api`.
   * @param opts.getAuthToken Returns the current in-memory access token (or null).
   * @param opts.onUnauthorized Invoked when a request returns 401 (after exhausting
   *   refresh attempts). Typically clears auth state and redirects to /login.
   */
  constructor(
    baseUrl = '/api',
    opts?: { getAuthToken?: () => string | null; onUnauthorized?: () => void | Promise<void> },
  ) {
    this.baseUrl = baseUrl;
    this.getAuthToken = opts?.getAuthToken;
    this.onUnauthorized = opts?.onUnauthorized;
  }

  /** Fetch the root document for `/api`. */
  async root(signal?: AbortSignal): Promise<HalDocument> {
    return this.get<HalDocument>(this.baseUrl, signal);
  }

  /** Resolve a templated URI by substituting params (RFC 6570-style, simplified). */
  expand(link: HalLink, params?: Record<string, string | number | undefined>): string {
    let href = link.href;
    if (!params) return href;
    for (const [key, value] of Object.entries(params)) {
      if (value === undefined) continue;
      href = href.replace(`{${key}}`, encodeURIComponent(String(value)));
      href = href.replace(`{?${key}}`, `?${key}=${encodeURIComponent(String(value))}`);
    }
    return href;
  }

  /** Follow a link by rel from a source document. */
  async follow(
    source: HalDocument,
    rel: string,
    params?: Record<string, string | number | undefined>,
    init?: RequestInit,
  ): Promise<HalDocument> {
    const link = this.link(source, rel);
    const href = this.expand(link, params);
    const method = (link.method ?? 'GET').toUpperCase();
    return this.get<HalDocument>(href, init?.signal, { ...init, method });
  }

  /** Pick a single link by rel from a document. Throws if missing. */
  link(source: HalDocument, rel: string): HalLink {
    const links = source._links;
    if (!links || !(rel in links)) {
      throw new Error(`HAL rel '${rel}' not present on document`);
    }
    const value = links[rel];
    return Array.isArray(value) ? value[0] : value;
  }

  private authHeaders(): Record<string, string> {
    const token = this.getAuthToken?.() ?? null;
    return token ? { Authorization: `Bearer ${token}` } : {};
  }

  public async get<T>(url: string, signal?: AbortSignal | null, init?: RequestInit): Promise<T> {
    const headers: Record<string, string> = { Accept: HAL_MEDIA, ...this.authHeaders(), ...(init?.headers as Record<string, string> | undefined) };
    const res = await fetch(url, {
      ...init,
      headers,
      ...(signal ? { signal } : {}),
    });
    if (res.status === 401 && this.onUnauthorized) {
      await this.onUnauthorized();
    }
    if (!res.ok) {
      const body = await res.text().catch(() => '');
      throw new HalError(res.status, res.statusText, body, url);
    }
    if (res.status === 204) return undefined as T;
    return (await res.json()) as T;
  }
}

/** Error thrown when a HAL request fails. Carries the URL and status for UI surfaces. */
export class HalError extends Error {
  readonly status: number;
  readonly statusText: string;
  readonly body: string;
  readonly url: string;

  constructor(status: number, statusText: string, body: string, url: string) {
    super(`HAL request failed: ${status} ${statusText} — ${url}`);
    this.name = 'HalError';
    this.status = status;
    this.statusText = statusText;
    this.body = body;
    this.url = url;
  }
}

/** Singleton client for the app. Auth wiring is attached at runtime via {@link configureHalClient}. */
export const halClient = new HalClient();

let isConfigured = false;

/**
 * Attach auth-aware behavior to the singleton HalClient. Safe to call multiple
 * times; subsequent calls are no-ops (the singleton keeps the first wiring).
 */
export function configureHalClient(opts: {
  getAuthToken: () => string | null;
  onUnauthorized: () => void | Promise<void>;
}): void {
  if (isConfigured) return;
  (halClient as unknown as { getAuthToken: () => string | null }).getAuthToken = opts.getAuthToken;
  (halClient as unknown as { onUnauthorized: () => void | Promise<void> }).onUnauthorized = opts.onUnauthorized;
  isConfigured = true;
}