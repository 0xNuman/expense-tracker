import { useState, useEffect } from 'react';
import type { Transaction } from '../hal/api';
import { ArrowUpRight, ArrowDownRight } from 'lucide-react';
import { useAuth } from '../auth/AuthContext';
import { fetchCategories } from '../features/categories/CategoriesTree';
import type { Category } from '../features/categories/CategoriesTree';
import { CategoryIconRenderer } from './IconPicker';

function dateLabel(occurredOn: string): string {
  const today = new Date();
  const d = new Date(occurredOn + 'T00:00:00');
  const startOfDay = (x: Date) => new Date(x.getFullYear(), x.getMonth(), x.getDate());
  const diffDays = Math.round((startOfDay(today).getTime() - startOfDay(d).getTime()) / 86_400_000);
  if (diffDays === 0) return 'Today';
  if (diffDays === 1) return 'Yesterday';
  return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
}

export function TransactionList({ transactions, onVoidRequested, onEditRequested }: { transactions: Transaction[]; onVoidRequested?: (t: Transaction) => void; onEditRequested?: (t: Transaction) => void }) {
  const { accessToken } = useAuth();
  const [categories, setCategories] = useState<Category[]>([]);
  const [showVoided, setShowVoided] = useState(false);

  useEffect(() => {
    if (accessToken) {
      fetchCategories(accessToken, true).then(setCategories).catch(console.error);
    }
  }, [accessToken]);

  const visibleTransactions = showVoided ? transactions : transactions.filter(t => !t.isVoided);

  if (visibleTransactions.length === 0) {
    return (
      <div className="flex flex-col gap-2">
        <div className="rounded-xl border border-dashed border-slate-300 bg-white p-6 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-400">
          No transactions yet. Add one to get started.
        </div>
        {transactions.length > 0 && (
          <button onClick={() => setShowVoided(true)} className="text-xs font-medium text-indigo-500 text-right hover:underline">Show voided</button>
        )}
      </div>
    );
  }

  const groups = new Map<string, Transaction[]>();
  for (const t of visibleTransactions) {
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
          <ul className="divide-y divide-slate-100 overflow-hidden glass rounded-2xl shadow-sm dark:divide-slate-800/60 dark:bg-slate-900/50">
            {items.map((t) => {
              const isIncome = t.type === 'Income';
              return (
                <li key={t.id} className={`group flex items-center justify-between px-4 py-3 hover:bg-slate-50 dark:hover:bg-slate-800/50 transition-colors ${t.isVoided ? 'opacity-50 grayscale' : ''}`}>
                  <div className="flex items-center gap-3">
                    <span
                      aria-hidden
                      className={`relative flex h-10 w-10 shrink-0 items-center justify-center rounded-xl ${
                        isIncome
                          ? 'bg-emerald-50 text-emerald-600 dark:bg-emerald-500/10 dark:text-emerald-400'
                          : 'bg-rose-50 text-rose-600 dark:bg-rose-500/10 dark:text-rose-400'
                      }`}
                    >
                      {(() => {
                         const cat = t.categoryId ? categories.find(c => c.id === t.categoryId) : undefined;
                         if (cat && cat.icon) {
                           return (
                             <>
                               <CategoryIconRenderer iconName={cat.icon} className="h-5 w-5" />
                               {isIncome ? <ArrowDownRight className="absolute -bottom-1 -right-1 h-4 w-4 rounded-full bg-white dark:bg-slate-900 p-[2px] text-emerald-600 dark:text-emerald-400" /> : <ArrowUpRight className="absolute -bottom-1 -right-1 h-4 w-4 rounded-full bg-white dark:bg-slate-900 p-[2px] text-rose-600 dark:text-rose-400" />}
                             </>
                           );
                         }
                         return isIncome ? <ArrowDownRight className="h-5 w-5" /> : <ArrowUpRight className="h-5 w-5" />;
                      })()}
                    </span>
                    <div>
                      <div className="flex items-center gap-2">
                        <p className={`text-sm font-medium ${t.isVoided ? 'line-through' : ''}`}>{t.memo ?? (isIncome ? 'Income' : 'Expense')}</p>
                        {t.isVoided && <span className="text-[10px] font-bold uppercase text-slate-400">Voided</span>}
                      </div>
                      <p className="text-xs text-slate-500 dark:text-slate-400">
                        {t.type} · {t.currency} {t.categoryId && categories.find(c => c.id === t.categoryId) && `· ${categories.find(c => c.id === t.categoryId)?.name}`}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4">
                    <div className="flex gap-2 opacity-0 transition-opacity group-hover:opacity-100 focus-within:opacity-100">
                      {!t.isVoided && onEditRequested && (
                        <button
                          type="button"
                          onClick={() => onEditRequested(t)}
                          className="rounded px-2 py-1 text-xs font-medium text-slate-500 hover:bg-slate-200 hover:text-slate-900 dark:hover:bg-slate-700 dark:hover:text-slate-100"
                        >
                          Edit
                        </button>
                      )}
                      {!t.isVoided && onVoidRequested && (
                        <button
                          type="button"
                          onClick={() => onVoidRequested(t)}
                          className="rounded px-2 py-1 text-xs font-medium text-rose-500 hover:bg-rose-100 hover:text-rose-700 dark:hover:bg-rose-900/30"
                        >
                          Delete
                        </button>
                      )}
                    </div>
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
      {transactions.some(t => t.isVoided) && (
        <div className="flex justify-end mt-2">
          <button
            type="button"
            onClick={() => setShowVoided(!showVoided)}
            className="text-xs font-medium text-slate-500 hover:text-slate-900 dark:hover:text-slate-300"
          >
            {showVoided ? 'Hide voided' : 'Show voided'}
          </button>
        </div>
      )}
    </div>
  );
}