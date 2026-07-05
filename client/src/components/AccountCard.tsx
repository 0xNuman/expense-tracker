import type { Account } from '../hal/api';
import { Banknote, Landmark, Vault, CreditCard, Wallet } from 'lucide-react';

const TypeIcon = ({ type }: { type: string }) => {
  switch (type) {
    case 'Cash':
      return <Banknote className="h-5 w-5 text-emerald-500 dark:text-emerald-400" />;
    case 'Checking':
      return <Landmark className="h-5 w-5 text-sky-500 dark:text-sky-400" />;
    case 'Savings':
      return <Vault className="h-5 w-5 text-indigo-500 dark:text-indigo-400" />;
    case 'CreditCard':
      return <CreditCard className="h-5 w-5 text-rose-500 dark:text-rose-400" />;
    case 'Prepaid':
      return <Wallet className="h-5 w-5 text-amber-500 dark:text-amber-400" />;
    default:
      return <Wallet className="h-5 w-5 text-slate-400" />;
  }
};

export function AccountCard({ account, onEditRequested, onArchiveRequested }: { account: Account; onEditRequested?: (account: Account) => void; onArchiveRequested?: (account: Account) => void }) {
  return (
    <div className="group relative flex flex-col justify-between rounded-2xl border border-slate-200/60 bg-white p-5 shadow-sm transition-all hover:-translate-y-1 hover:shadow-lg dark:border-slate-800/60 dark:bg-slate-900/80">
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-slate-50 dark:bg-slate-800/50">
            <TypeIcon type={account.type} />
          </div>
          <div>
            <p className="font-semibold text-slate-900 dark:text-slate-100">{account.name}</p>
            <p className="text-xs font-medium text-slate-500 dark:text-slate-400">
              {account.type}
            </p>
          </div>
        </div>
        <div className="flex flex-col items-end">
          <p className="font-mono text-lg font-medium tracking-tight text-slate-900 dark:text-slate-100">
            {account.balance.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </p>
          <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">{account.currency}</p>
        </div>
      </div>
      
      {/* Hover Actions */}
      <div className="absolute right-4 top-1/2 flex -translate-y-1/2 items-center gap-1 opacity-0 transition-opacity focus-within:opacity-100 group-hover:opacity-100 bg-white/90 dark:bg-slate-900/90 backdrop-blur-sm rounded-lg p-1 shadow-sm border border-slate-100 dark:border-slate-800">
        {onEditRequested && (
          <button
            type="button"
            className="rounded-md px-2.5 py-1.5 text-xs font-semibold text-slate-600 transition-colors hover:bg-slate-100 hover:text-slate-900 dark:text-slate-300 dark:hover:bg-slate-800 dark:hover:text-slate-100"
            onClick={(e) => { e.preventDefault(); onEditRequested(account); }}
          >
            Edit
          </button>
        )}
        {!account.isArchived && onArchiveRequested && (
          <button
            type="button"
            className="rounded-md px-2.5 py-1.5 text-xs font-semibold text-rose-600 transition-colors hover:bg-rose-50 hover:text-rose-700 dark:text-rose-400 dark:hover:bg-rose-900/30"
            onClick={(e) => { e.preventDefault(); onArchiveRequested(account); }}
          >
            Archive
          </button>
        )}
      </div>

      {account.isArchived && (
        <span className="absolute -top-2 right-4 inline-flex items-center rounded-full border border-amber-200 bg-amber-50 px-2 py-0.5 text-[10px] font-bold uppercase tracking-widest text-amber-600 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-400">
          Archived
        </span>
      )}
    </div>
  );
}