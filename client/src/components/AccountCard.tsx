import type { Account } from '../hal/api';

const TYPE_GLYPH: Record<string, string> = {
  Cash: '$',
  Checking: '🏦',
  Savings: '🐷',
  CreditCard: '💳',
  Prepaid: '🎫',
};

export function AccountCard({ account, onRenameRequested, onArchiveRequested }: { account: Account; onRenameRequested?: (account: Account) => void; onArchiveRequested?: (account: Account) => void }) {
  const glyph = TYPE_GLYPH[account.type] ?? '•';
  return (
    <div className="group flex flex-col justify-between rounded-xl border border-slate-200 bg-white p-4 shadow-sm transition-all hover:shadow-md dark:border-slate-800 dark:bg-slate-900">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <span aria-hidden className="text-lg">
            {glyph}
          </span>
          <div>
            <div className="flex items-center gap-2">
              <p className="font-medium leading-tight">{account.name}</p>
              <div className="flex items-center gap-2 opacity-0 transition-opacity group-hover:opacity-100 focus-within:opacity-100">
                {onRenameRequested && (
                  <button
                    type="button"
                    className="rounded px-2 py-1 text-xs font-medium text-slate-500 hover:bg-slate-200 hover:text-slate-900 dark:hover:bg-slate-700 dark:hover:text-slate-100"
                    onClick={() => onRenameRequested(account)}
                  >
                    Rename
                  </button>
                )}
                {!account.isArchived && onArchiveRequested && (
                  <button
                    type="button"
                    className="rounded px-2 py-1 text-xs font-medium text-rose-500 hover:bg-rose-100 hover:text-rose-700 dark:hover:bg-rose-900/30"
                    onClick={() => onArchiveRequested(account)}
                  >
                    Archive
                  </button>
                )}
              </div>
            </div>
            <p className="text-xs text-slate-500 dark:text-slate-400">
              {account.type} · {account.currency}
            </p>
          </div>
        </div>
        <div className="text-right">
          <p className="font-mono text-lg tabular-nums">
            {account.balance.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          </p>
          <p className="text-xs text-slate-400">{account.currency} balance</p>
        </div>
      </div>
      {account.isArchived && (
        <span className="mt-2 inline-block rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-700 dark:bg-amber-950 dark:text-amber-300">
          Archived
        </span>
      )}
    </div>
  );
}