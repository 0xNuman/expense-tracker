import type { Transaction } from '../hal/api';

function dateLabel(occurredOn: string): string {
  const today = new Date();
  const d = new Date(occurredOn + 'T00:00:00');
  const startOfDay = (x: Date) => new Date(x.getFullYear(), x.getMonth(), x.getDate());
  const diffDays = Math.round((startOfDay(today).getTime() - startOfDay(d).getTime()) / 86_400_000);
  if (diffDays === 0) return 'Today';
  if (diffDays === 1) return 'Yesterday';
  return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
}

export function TransactionList({ transactions, onVoidRequested }: { transactions: Transaction[]; onVoidRequested?: (t: Transaction) => void }) {
  if (transactions.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-slate-300 bg-white p-6 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-400">
        No transactions yet. Add one to get started.
      </div>
    );
  }

  const groups = new Map<string, Transaction[]>();
  for (const t of transactions) {
    const label = dateLabel(t.occurredOn);
    const arr = groups.get(label) ?? [];
    arr.push(t);
    groups.set(label, arr);
  }

  return (
    <div className="flex flex-col gap-4">
      {Array.from(groups.entries()).map(([label, items]) => (
        <section key={label}>
          <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
            {label}
          </h3>
          <ul className="divide-y divide-slate-100 overflow-hidden rounded-xl border border-slate-200 bg-white dark:divide-slate-800 dark:border-slate-800 dark:bg-slate-900">
            {items.map((t) => {
              const isIncome = t.type === 'Income';
              return (
                <li key={t.id} className={`flex items-center justify-between px-4 py-3 ${t.isVoided ? 'opacity-50 grayscale' : ''}`}>
                  <div className="flex items-center gap-3">
                    <span
                      aria-hidden
                      className={`flex h-9 w-9 items-center justify-center rounded-full text-sm ${
                        isIncome
                          ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300'
                          : 'bg-rose-100 text-rose-700 dark:bg-rose-950 dark:text-rose-300'
                      }`}
                    >
                      {isIncome ? '↑' : '↓'}
                    </span>
                    <div>
                      <div className="flex items-center gap-2">
                        <p className={`text-sm font-medium ${t.isVoided ? 'line-through' : ''}`}>{t.memo ?? (isIncome ? 'Income' : 'Expense')}</p>
                        {t.isVoided && <span className="text-[10px] font-bold uppercase text-slate-400">Voided</span>}
                      </div>
                      <p className="text-xs text-slate-500 dark:text-slate-400">
                        {t.type} · {t.currency}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    {!t.isVoided && onVoidRequested && (
                      <button
                        type="button"
                        onClick={() => onVoidRequested(t)}
                        className="text-[10px] uppercase tracking-wider text-slate-400 hover:text-rose-600 dark:hover:text-rose-400 opacity-0 group-hover:opacity-100 transition-opacity"
                        style={{ opacity: 1 /* fallback for simple hover without group */ }}
                      >
                        Void
                      </button>
                    )}
                    <span
                      className={`font-mono text-sm tabular-nums ${
                        isIncome ? 'text-emerald-600 dark:text-emerald-400' : 'text-rose-600 dark:text-rose-400'
                      } ${t.isVoided ? 'line-through' : ''}`}
                    >
                      {isIncome ? '+' : '−'}
                      {t.amount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </span>
                  </div>
                </li>
              );
            })}
          </ul>
        </section>
      ))}
    </div>
  );
}