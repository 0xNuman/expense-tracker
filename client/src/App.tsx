import { useEffect, useState } from 'react'
import { halClient, type HalDocument } from './hal/HalClient'

interface ApiRootState {
  loading: boolean;
  error: string | null;
  document: HalDocument | null;
}

function useApiRoot(): ApiRootState {
  const [state, setState] = useState<ApiRootState>({ loading: true, error: null, document: null });

  useEffect(() => {
    const controller = new AbortController();
    setState({ loading: true, error: null, document: null });
    halClient
      .root(controller.signal)
      .then((document) => setState({ loading: false, error: null, document }))
      .catch((err: unknown) => {
        if (controller.signal.aborted) return;
        setState({ loading: false, error: err instanceof Error ? err.message : 'Unknown error', document: null });
      });
    return () => controller.abort();
  }, []);

  return state;
}

function App() {
  const { loading, error, document } = useApiRoot();

  return (
    <div className="min-h-full bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100">
      <div className="mx-auto flex min-h-full max-w-3xl flex-col gap-6 p-6 sm:p-10">
        <header className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold tracking-tight sm:text-3xl">Expense Tracker</h1>
            <p className="text-sm text-slate-500 dark:text-slate-400">Walking skeleton · HAL discovery</p>
          </div>
          <span className="rounded-full bg-emerald-100 px-3 py-1 text-xs font-medium text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300">
            Phase 1
          </span>
        </header>

        <main className="flex-1">
          {loading && (
            <div className="flex items-center gap-3 rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
              <span className="h-2 w-2 animate-pulse rounded-full bg-sky-500" />
              <p className="text-sm text-slate-600 dark:text-slate-300">Walking to <code className="font-mono">/api</code>…</p>
            </div>
          )}

          {error && (
            <div className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700 dark:border-rose-900 dark:bg-rose-950 dark:text-rose-200">
              <p className="font-medium">Could not reach the API</p>
              <p className="mt-1 font-mono text-xs">{error}</p>
            </div>
          )}

          {document && <ApiLinks document={document} />}
        </main>

        <footer className="text-center text-xs text-slate-400">
          Mobile-first React + Vite + Tailwind on a .NET 10 Minimal API with HAL hypermedia.
        </footer>
      </div>
    </div>
  )
}

function ApiLinks({ document }: { document: HalDocument }) {
  const links = (document._links ?? {}) as Record<string, unknown>;
  const entries = Object.entries(links).filter(([rel]) => rel !== 'curies');

  return (
    <div className="flex flex-col gap-4">
      <div className="rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
        <p className="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">Root state</p>
        <dl className="mt-2 grid grid-cols-1 gap-2 sm:grid-cols-3">
          <Field label="name" value={String(document.name ?? '—')} />
          <Field label="version" value={String(document.version ?? '—')} />
          <Field label="phase" value={String(document.phase ?? '—')} />
        </dl>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
        <p className="text-xs uppercase tracking-wide text-slate-500 dark:text-slate-400">
          Discoverable links ({entries.length})
        </p>
        <ul className="mt-2 divide-y divide-slate-100 dark:divide-slate-800">
          {entries.map(([rel, value]) => {
            const link = Array.isArray(value) ? value[0] : value;
            return (
              <li key={rel} className="flex flex-col gap-1 py-2 sm:flex-row sm:items-center sm:justify-between">
                <span className="font-mono text-sm text-slate-700 dark:text-slate-200">{rel}</span>
                <span className="font-mono text-xs text-slate-500 dark:text-slate-400">
                  {link.method ?? 'GET'} · {link.href}
                </span>
              </li>
            );
          })}
        </ul>
      </div>
    </div>
  );
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-xs text-slate-500 dark:text-slate-400">{label}</dt>
      <dd className="font-mono text-sm">{value}</dd>
    </div>
  );
}

export default App